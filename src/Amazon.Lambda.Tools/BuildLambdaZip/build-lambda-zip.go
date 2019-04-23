// This utility was adapted from https://github.com/aws/aws-lambda-go/blob/master/cmd/build-lambda-zip/main.go
// It allows Amazon.Lambda.Tools to zip files on Windows and unzip them on linux (AWS Lambda) with the correct file permissions.
// The .NET zip libraries do not allow you to do this so we're forced to do it in a language that supports it.
// This repo includes a compiled version of this utility at /Resources/build-lambda-zip.exe; so you don't need to install Go to build this repo.
// If you do need to update this utility you have to install Go and run 'dotnet msbuild -target:build-lambda-zip'.
package main

import (
	"archive/zip"
	"bufio"
	"errors"
	"fmt"
	"io/ioutil"
	"os"
	"path/filepath"
	"strings"

	"gopkg.in/urfave/cli.v1"
)

func main() {
	app := cli.NewApp()
	app.Name = "build-lambda-zip"
	app.Usage = "Put files into a zip file that works with AWS Lambda."
	app.Flags = []cli.Flag{
		cli.StringFlag{
			Name:  "output, o",
			Value: "",
			Usage: "output file path for the zip. Defaults to the first input file name.",
		},
		cli.StringFlag{
			Name:  "input, i",
			Value: "",
			Usage: "input file path for the list of files to zip.",
		},
	}

	app.Action = func(c *cli.Context) error {
		firstArg := c.Args().First()
		outputZip := c.String("output")
		if outputZip == "" {
			outputZip = fmt.Sprintf("%s.zip", filepath.Base(firstArg))
		}

		inputTxt := c.String("input")
		var files = c.Args()
		if inputTxt != "" {
			//Add any files from the input txt file to the files list
			lines, err := loadInputFiles(inputTxt)
			if err != nil {
				return fmt.Errorf("Failed to load files to zip: %v", err)
			}

			files = append(files, lines...)
		}

		if len(files) == 0 {
			return errors.New("No input files provided")
		}

		if err := compressExeAndArgs(outputZip, files); err != nil {
			return fmt.Errorf("Failed to compress file: %v", err)
		}
		return nil
	}

	if err := app.Run(os.Args); err != nil {
		fmt.Fprintf(os.Stderr, "%v\n", err)
		os.Exit(1)
	}
}

func writeExe(writer *zip.Writer, pathInZip string, data []byte) error {
	exe, err := writer.CreateHeader(&zip.FileHeader{
		CreatorVersion: 3 << 8,     // indicates Unix
		ExternalAttrs:  0777 << 16, // -rwxrwxrwx file permissions
		Name:           pathInZip,
		Method:         zip.Deflate,
	})
	if err != nil {
		return err
	}

	_, err = exe.Write(data)
	return err
}

func compressExeAndArgs(outZipPath string, files []string) error {
	zipFile, err := os.Create(outZipPath)
	if err != nil {
		return err
	}
	defer func() {
		closeErr := zipFile.Close()
		if closeErr != nil {
			fmt.Fprintf(os.Stderr, "Failed to close zip file: %v\n", closeErr)
		}
		return
	}()

	zipWriter := zip.NewWriter(zipFile)
	defer zipWriter.Close()

	for _, filename := range files {
		data, err := ioutil.ReadFile(filename)
		if err != nil {
			return err
		}

		linuxName := strings.Replace(filename, "\\", "/", -1)
		fmt.Println(linuxName)
		err = writeExe(zipWriter, linuxName, data)
		if err != nil {
			return err
		}
	}
	return err
}

func loadInputFiles(inputFilename string) ([]string, error) {
	file, err := os.Open(inputFilename)
	if err != nil {
		return nil, err
	}
	defer file.Close()

	var lines []string
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		lines = append(lines, scanner.Text())
	}
	return lines, scanner.Err()
}

// This utility was adapted from https://github.com/aws/aws-lambda-go/blob/master/cmd/build-lambda-zip/main.go
// It allows Amazon.Lambda.Tools to zip files on Windows and unzip them on linux (AWS Lambda) with the correct file permissions.
// The .NET zip libraries do not allow you to do this so we're forced to do it in a language that supports it.
// This repo includes a compiled version of this utility at /Resources/build-lambda-zip.exe; so you don't need to install Go to build this repo.
// If you do need to update this utility you have to install Go and run 'dotnet msbuild -target:build-lambda-zip'.
package main

import (
	"archive/zip"
	"errors"
	"fmt"
	"io/ioutil"
	"os"
	"path/filepath"

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
	}

	app.Action = func(c *cli.Context) error {
		if !c.Args().Present() {
			return errors.New("No input provided")
		}

		firstArg := c.Args().First()
		outputZip := c.String("output")
		if outputZip == "" {
			outputZip = fmt.Sprintf("%s.zip", filepath.Base(firstArg))
		}

		if err := compressExeAndArgs(outputZip, c.Args()); err != nil {
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

func compressExeAndArgs(outZipPath string, args []string) error {
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


	for _, arg := range args {
		data, err := ioutil.ReadFile(arg)
		if err != nil {
			return err
		}

		err = writeExe(zipWriter, arg, data)
		if err != nil {
			return err
		}
	}
	return err
}

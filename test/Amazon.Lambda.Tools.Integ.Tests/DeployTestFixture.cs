using Amazon.S3;
using Amazon.S3.Util;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.Lambda.Tools.Integ.Tests
{
    public class DeployTestFixture : IDisposable
    {
        public string Bucket { get; set; }
        public IAmazonS3 S3Client { get; set; }

        public DeployTestFixture()
        {
            this.S3Client = new AmazonS3Client(RegionEndpoint.GetBySystemName(TestConstants.TEST_REGION));

            this.Bucket = "dotnet-lambda-tests-" + DateTime.Now.Ticks;

            S3Client.PutBucketAsync(this.Bucket).GetAwaiter().GetResult();
        }

        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    AmazonS3Util.DeleteS3BucketWithObjectsAsync(this.S3Client, this.Bucket).GetAwaiter().GetResult();

                    this.S3Client.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

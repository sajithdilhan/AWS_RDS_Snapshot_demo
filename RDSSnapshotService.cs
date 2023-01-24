using Amazon;
using Amazon.RDS;
using Amazon.RDS.Model;
using Microsoft.Extensions.Configuration;

namespace AWS_Snapshot_demo
{
    public class RDSSnapshotService 
    {
        public RDSSnapshotService(IConfiguration configuration)
        {
            _configuration = configuration; // Inject config dependency
        }

        public async Task<bool> CreateSnapshot(string instanceId)
        {
            bool snapshotCompleted = false;

            string snapshotIdentifier = $"{instanceId}{DateTime.Now.ToString("ddMMyyyyHHmmss")}";
            Console.WriteLine($"Please wait while creating snapshot:{snapshotIdentifier}");

            var result = await TakeSnapshotAsync(instanceId, snapshotIdentifier);
            if (result != null && result.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                DescribeDBSnapshotsResponse snapshotStatus = new();

                do
                {
                    Thread.Sleep(15000);
                    snapshotStatus = GetSnapshotStatus().Result;
                    if (snapshotStatus.DBSnapshots.FirstOrDefault(s => s.DBSnapshotIdentifier == snapshotIdentifier)?.Status == "available")
                    {
                        snapshotCompleted = true;
                    }
                    var progress = snapshotStatus.DBSnapshots.FirstOrDefault(s => s.DBSnapshotIdentifier == snapshotIdentifier);
                    Console.WriteLine($"Snapshot status:{progress?.Status} and progress:{progress?.PercentProgress}%");
                }
                while (!snapshotCompleted);

                return snapshotCompleted;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error occurred during RDS API call. HTTP status code:{result?.HttpStatusCode}");
                return false;
            }
        }

        private async Task<CreateDBSnapshotResponse> TakeSnapshotAsync(string instanceId, string snapshotIdentifier)
        {
            var rdsClient = GetRDSClient();
            return await rdsClient.CreateDBSnapshotAsync(new CreateDBSnapshotRequest(snapshotIdentifier, instanceId));
        }

        private async Task<DescribeDBSnapshotsResponse> GetSnapshotStatus()
        {
            var rdsClient = GetRDSClient();
            return await rdsClient.DescribeDBSnapshotsAsync(new DescribeDBSnapshotsRequest());
        }

        private AmazonRDSClient GetRDSClient()
        {
            var awsKey = _configuration["AWSAccessKey"]; // Read from appsettings
            var awsSecret = _configuration["AWSSecretAccesskey"]; // Read from appsettings
            var endpoint = _configuration["AWSRegionEndpoint"]; // Read from appsettings
            var regionEndPoint = RegionEndpoint.GetBySystemName(endpoint) != null ? RegionEndpoint.GetBySystemName(endpoint) : RegionEndpoint.APSoutheast1;
            var rdsClient = new AmazonRDSClient(awsKey, awsSecret, regionEndPoint);
            return rdsClient;
        }
    }
}


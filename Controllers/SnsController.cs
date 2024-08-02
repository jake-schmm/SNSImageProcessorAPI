using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace RestAPI.Controllers
{
    [Route("api/SNS")]
    [ApiController]
    public class SNSController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;

        public SNSController(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] dynamic snsMessage)
        {
            var messageType = Request.Headers["x-amz-sns-message-type"].FirstOrDefault();

            if (messageType == "SubscriptionConfirmation")
            {
                var subscribeUrl = snsMessage.SubscribeURL.ToString();
                // Handle the SubscriptionConfirmation here, you need to confirm the subscription by sending a GET request to the SubscribeURL
                // When you subscribe an endpoint (like an HTTP/HTTPS URL, email address, or SQS queue) to an SNS topic, SNS sends a subscription confirmation message to the endpoint.
                // The recipient (or the application) must visit the SubscribeURL to confirm the subscription. This is typically done by sending a GET request to the URL provided in the confirmation message.
                await ConfirmSubscription(subscribeUrl);
                return Ok();
            }

            if (messageType == "Notification")
            {
                var message = JsonConvert.DeserializeObject<SnsMessage>(snsMessage["Message"].ToString());
                await ProcessMessageAsync(message);
                return Ok();
            }

            return BadRequest();
        }

        private async Task ConfirmSubscription(string subscribeUrl)
        {
            using (var httpClient = new HttpClient())
            {
                await httpClient.GetAsync(subscribeUrl);
            }
        }

        private async Task ProcessMessageAsync(SnsMessage message)
        {
            var bucketName = message.BucketName;
            var key = message.S3Key;
            var localFilePath = Path.Combine("C:/Uploads", Path.GetFileName(key));

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.DownloadAsync(localFilePath, bucketName, key);

            Console.WriteLine($"Downloaded resized image to {localFilePath}");
        }
    }

}
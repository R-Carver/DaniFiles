using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace WebApplication1.Utilities
{
    public class BlobUtility
    {
        public CloudStorageAccount storageAccount;
        public BlobUtility(string AccountName, string AccountKey)
        {
            string UserConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", AccountName, AccountKey);
            storageAccount = CloudStorageAccount.Parse(UserConnectionString);
        }


        public CloudBlockBlob UploadBlob(string BlobName, string ContainerName, Stream stream)
        {
            //Creating an object on CloudBlobClient
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            //Getting the reference of the container
            CloudBlobContainer container = blobClient.GetContainerReference(ContainerName.ToLower());
            //Creating an object of block BLOB
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(BlobName);
            // blockBlob.UploadFromByteArray()
            try
            {
                //Uploading an image stream in the block BLOB
                blockBlob.UploadFromStream(stream);
                return blockBlob;
            }
            catch (Exception e)
            {
                var r = e.Message;
                return null;
            }


        }


        public void DeleteBlob(string BlobName, string ContainerName)
        {

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(ContainerName);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(BlobName);
            blockBlob.Delete();
        }


        public CloudBlockBlob DownloadBlob(string BlobName, string ContainerName)
        {
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(ContainerName);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(BlobName);
            // blockBlob.DownloadToStream(Response.OutputStream);
            return blockBlob;
        }
    }
}
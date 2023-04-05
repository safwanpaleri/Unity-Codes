using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.IO;
using UnityEngine.UI;
using UnityEngine.Networking;

public class AWSManager : MonoBehaviour
{
    // AWS credentials and configuration settings
    private string identityPoolId = ""; // Cognito identity pool ID
    private string bucketName = "Your-Bucket-Name"; // Name of your S3 bucket
    private string Region = RegionEndpoint.USWest1.SystemName; // Region in which your S3 bucket is located
    private Texture2D defaultTex; // Default texture to use for images

    // Singleton instance
    private static AWSManager _instance;

    // Path to store downloaded files
    private string path;

    // AWS credentials
    public CognitoAWSCredentials _credentials;
    public CognitoAWSCredentials Credentials
    {
        get
        {
            if (_credentials == null)
            {
                // Initialize credentials with the Cognito identity pool ID and region
                _credentials = new CognitoAWSCredentials(identityPoolId, RegionEndpoint.USWest1);
            }
            return _credentials;
        }
    }

    // Cognito identity ID
    public string identityID;

    // S3 client and configuration
    public string S3Region = RegionEndpoint.USWest1.SystemName;
    public RegionEndpoint _S3Region
    {
        get
        {
            return RegionEndpoint.GetBySystemName(S3Region);
        }
    }
    public AmazonS3Client _S3Client;
    public AmazonS3Client S3Client
    {
        get
        {
            if (_S3Client == null)
                // Initialize S3 client with the Cognito credentials and S3 region
                _S3Client = new AmazonS3Client(new CognitoAWSCredentials(identityPoolId, RegionEndpoint.USWest1), _S3Region);

            return _S3Client;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Set up singleton instance
        _instance = this;

        // Initialize the AWS SDK for Unity
        UnityInitializer.AttachToGameObject(this.gameObject);
        try
        {
            AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }

        // Get the Cognito identity ID for this user
        Credentials.GetIdentityIdAsync(delegate (AmazonCognitoIdentityResult<string> result)
        {
            if (result.Exception != null)
                Debug.Log(result.Exception);
            identityID = result.Response;
        });

        // Set up references to other scripts
        path = Path.Combine(Application.persistentDataPath, "changename.png");
        authUiManager = FindObjectOfType<AuthUiManager>();
        gameAuth = FindObjectOfType<GameAuth>();
    }

    // Upload a file to S3
    public void UploadFile(FileStream fs, string filename)
    {
        PostObjectRequest request = new PostObjectRequest()
        {
            Bucket = bucketName, // Name of your S3 bucket
            Key = filename, // Name of the file to upload
            InputStream = fs, // Stream containing the file data
            CannedACL = S3CannedACL.Private, // Set the file access level to private
            Region = _S3Region // Set the S3 region
        };
        // Post the object to the S3 bucket asynchronously
        S3Client.PostObjectAsync(request, (responseObject) =>
        {
            if (responseObject.Exception == null)
            {
                // If the upload is successful
                Debug.Log("Success!");
            }
            else
                Debug.LogError("Failed! : " + responseObject.Exception);
        });

    }
    // Downloads a Image from the S3 bucket and returns it as a Texture2D 
    //Also can be used to download other files, just provide correct filename with extension
    public Texture2D DownloadFile(string filename)
    {
        // Create a new Texture2D to store the downloaded image
        Texture2D tex = new Texture2D(2, 2);
        // Create a ListObjectsRequest to get a list of all objects in the bucket
        var request = new ListObjectsRequest()
        {
            BucketName = bucketName
        };
        // Send the ListObjects request to S3 asynchronously
        S3Client.ListObjectsAsync(request, (responseObj) =>
        {
            byte[] data = null;
            // If the ListObjects request was successful, send a GetObject request to download the specified file
            S3Client.GetObjectAsync(bucketName, filename, (responseObject) =>
            {
                if (responseObject.Exception == null)
                {
                    // Read the response stream and write it to a MemoryStream
                    using (StreamReader sr = new StreamReader(responseObject.Response.ResponseStream))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            var buffer = new byte[512];
                            var bytesRead = default(int);
                            while ((bytesRead = sr.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                                ms.Write(buffer, 0, bytesRead);
                            // Convert the MemoryStream to a byte array and load it into the Texture2D
                            data = ms.ToArray();
                            tex.LoadImage(data);
                        }
                    }
                }
            });
        });
        // Return the downloaded Texture2D
        return tex;
    }

    // Deletes a file from the S3 bucket
    public void DeleteFile(string filename)
    {
        // Create a new DeleteObjectRequest to delete the specified file
        var request = new DeleteObjectRequest()
        {
            BucketName = bucketName,
            Key = filename
        };
        // Send the DeleteObject request to S3 asynchronously
        S3Client.DeleteObjectAsync(request, (responseObj) =>
        {
            if (responseObj.Exception != null)
                Debug.LogError("Failed to delete");
        });
    }
    public void DeleteAccountFolder(int profile_id)
    {
        // Specify the name of the file along with the folder
        string filename = "Name of file along with folder";

        // Create a list to hold all the objects in the S3 bucket
        List<S3Object> s3objects = new List<S3Object>();

        // Create a request object to list all the objects in the specified folder
        ListObjectsRequest request = new ListObjectsRequest
        {
            BucketName = bucketName,
            Prefix = "folder name" // Provide the full path to the folder, if it is inside another folder
        };

        // Call the ListObjectsAsync method to retrieve a list of objects in the bucket
        S3Client.ListObjectsAsync(request, (responseObject) =>
        {
            // Store the objects in the s3objects list
            s3objects = responseObject.Response.S3Objects;

            // Create a request object to delete all the objects in the list
            DeleteObjectsRequest request2 = new DeleteObjectsRequest();
            foreach (S3Object entry in s3objects)
            {
                request2.AddKey(entry.Key);
            }
            request2.BucketName = bucketName;

            // Call the DeleteObjectsAsync method to delete all the objects in the list
            S3Client.DeleteObjectsAsync(request2, (responseObj) =>
            {
                if (responseObj.Exception != null)
                    Debug.LogError("Deletion Failed!");
            });
        });
    }

    public bool changeName(string filename1, string filename2)
    {
        bool iscomplete = false;

        // Create a request object to list all the objects in the bucket
        var request = new ListObjectsRequest()
        {
            BucketName = bucketName
        };

        // Call the ListObjectsAsync method to retrieve a list of objects in the bucket
        S3Client.ListObjectsAsync(request, (responseObj) =>
        {
            byte[] data = null;

            // Use the GetObjectAsync method to retrieve an object from the bucket
            S3Client.GetObjectAsync(bucketName, filename1, (responseObject) =>
            {
                if (responseObject.Exception == null)
                {
                    // Read the response stream and write it to a file
                    using (StreamReader sr = new StreamReader(responseObject.Response.ResponseStream))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            FileStream fs = new FileStream(Path.Combine(Application.persistentDataPath, "changename.png"), FileMode.OpenOrCreate);
                            var buffer = new byte[512];
                            var bytesRead = default(int);
                            while ((bytesRead = sr.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                fs.Write(buffer, 0, bytesRead);
                                ms.Write(buffer, 0, bytesRead);
                            }
                            fs.Close();
                            fs.Dispose();

                            // Upload the file with the new name and delete the original file
                            FileStream fs2 = new FileStream(path, FileMode.Open);
                            UploadFile(fs2, filename2);
                            DeleteFile(filename1);

                            iscomplete = true;
                        }
                    }
                }
                else
                    Debug.LogError("Failed!: " + filename1);
            });
        });
        return iscomplete;
    }

    // This function checks if an object with a specified filename exists in the S3 bucket
    public bool isObjectPresent(string filename1)
    {
        bool iscomplete = false; // Flag to indicate if S3 request has completed
        bool isSuccess = false; // Flag to indicate if S3 request was successful
        bool doOnce = false; // Flag to prevent infinite loop

        var request = new ListObjectsRequest() // Create a request to list all objects in the bucket
        {
            BucketName = bucketName
        };
        S3Client.ListObjectsAsync(request, (responseObj) => // Asynchronously call S3 ListObjects API
        {
            byte[] data = null;
            S3Client.GetObjectAsync(bucketName, filename1, (responseObject) => // Asynchronously call S3 GetObject API for the specified object
            {
                if (responseObject.Exception == null)
                {
                    isSuccess = true; // Set flag to indicate success
                }
                iscomplete = true; // Set flag to indicate completion of request
            });
        });

        // Wait for the S3 request to complete before returning isSuccess
        while (!iscomplete)
        {
            // Do nothing and wait for iscomplete to become true
        }

        return isSuccess;
    }

    public AudioClip audioToSend;
    bool isCompleted = false;

    // This function downloads an audio file with a specified filename from the  S3 bucket and returns it as an AudioClip
    public AudioClip DownloadAudio(string filename, string objName)
    {
        var request = new ListObjectsRequest()
        {
            BucketName = bucketName
        };

        // Asynchronously call S3 ListObjects API
        S3Client.ListObjectsAsync(request, (responseObj) =>
        {
            byte[] data = null;
            // Asynchronously call S3 GetObject API for the specified object
            S3Client.GetObjectAsync(bucketName, filename, (responseObject) =>
            {
                if (responseObject.Exception == null)
                {
                    using (StreamReader sr = new StreamReader(responseObject.Response.ResponseStream))
                    {
                        // Create a FileStream to write the downloaded file to disk
                        FileStream fs = new FileStream(Path.Combine(Application.persistentDataPath, objName), FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        var buffer = new byte[512];
                        var bytesRead = default(int);
                        while ((bytesRead = sr.BaseStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fs.Write(buffer, 0, bytesRead); // Write the downloaded file to the FileStream
                        }
                        fs.Close(); // Close the FileStream
                        fs.Dispose(); // Dispose of the FileStream resources
                        StartCoroutine(GetAudioClip(Path.Combine(Application.persistentDataPath, objName), filename)); // Asynchronously load the AudioClip from the downloaded file
                    }
                }
                else
                {
                    Debug.LogError("Failed!: " + filename); // Log an error message if the S3 request fails
                }
            });
        });

        Debug.LogWarning("last Line of coroutine");

        return audioToSend; // Return the AudioClip, which may be null if the download and load operations have not completed
    }

    // This method loads an audio clip from the given file path and assigns it to the 'audioToSend' variable.
    // The filename is also assigned to the audio clip's name property.
    private IEnumerator GetAudioClip(string filePath, string filename)
    {
        // Create a UnityWebRequest object to load the audio clip from the specified file path.
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV))
        {
            // Send the request and wait for the response to complete.
            yield return www.SendWebRequest();

            // If there was an error, log the error message to the console.
            if (www.error != null)
            {
                Debug.Log(www.error);
            }
            // Otherwise, assign the downloaded audio clip to the 'audioToSend' variable and set its name property to the given filename.
            else
            {
                audioToSend = ((DownloadHandlerAudioClip)www.downloadHandler).audioClip;
                audioToSend.name = filename;
            }
        }
    }

}
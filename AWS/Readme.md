# AWS Manager for Unity

AWS Manager is a Unity script for managing file uploads and downloads to Amazon S3 using the AWS SDK for Unity. It provides a simple interface for uploading and downloading files to and from S3.

## Getting Started
## Prerequisites
* Unity 2018.1 or higher
* AWS SDK for Unity

## Installation
Clone or download the repository.
Import the AWS SDK for Unity into your Unity project.
Add the AWSManager.cs script to your Unity project.

## Configuration
Before using the script, you need to configure it with your AWS credentials and S3 bucket information. Open the AWSManager.cs script and modify the following variables:

```
identityPoolId: Your Cognito identity pool ID.
bucketName: The name of your S3 bucket.
Region: The region in which your S3 bucket is located.
```
## Usage
### Uploading a File
To upload a file to S3, call the 'UploadFile' method and pass in a FileStream object containing the file data and the filename:

```csharp
FileStream fs = new FileStream("path/to/file", FileMode.Open, FileAccess.Read);
string filename = "myFile.txt";
AWSManager.Instance.UploadFile(fs, filename);
```

### Downloading a Image
To download a file from S3, call the DownloadFile method and pass in the filename:

```csharp
string filename = "myFile.jpg";
Texture2D tex = AWSManager.Instance.DownloadFile(filename);
```
The DownloadFile method returns a Texture2D object containing the downloaded file data. You can use the same method to download other files like txt by modifing the return type and sending 'filename' with the exact extension


### Is Object Present
this function checks whether a file, image or anything is present in the bucket or not. It returns a bool accordingly.

```csharp
string filename = "NameofFile.txt";
bool isPresent = AWSManager.Instance.isObjectPresent(filename);
```

### Change Name
This function is used to change the name of the file in the bucket. Added a bool to be return as it took sometime to change the name.
NOTE: make sure the extension is exact otherwise the file will not be usable.

```csharp
string NameToChange = "OldName.png";
string NewNameOfFile = "NewName.png";
bool isCompleted = AWSManager.Instance.changeName(NameToChange, NewNameOfFile);
```

### Get AudioCLip
Getting audio clip was different from fetching other files, so had to create a seperate function for it.
There are two name, 1. 'AudioName' the name saved in the bucket, 2. DownloadName the name to be assigned after downloading.
NOTE: MAKE SURE THE EXTENSION ARE EXACTLY SAME.

```csharp
string AudioName = "NameOfAudioInBucket.mp3";
string DownloadName = "NameToAddedToThisAudioAfterDownloading.mp3"
AudioClip audioClip = AWSManager.Instance.DownloadAudio(AudioName, DownloadName);
```

### Deleting a File
To delete a file from S3, call the DeleteFile method and pass in the filename:

```csharp
string filename = "myFile.txt";
AWSManager.Instance.DeleteFile(filename);
```

### Deleting Account Folder
to delete a user's account folder from the bucket, you can use this function
currently in my bucket the folder is saved according to unique identification id, so the foldername would be that
if you have different way of saving each users data, give the foldername which has data of that user

```csharp
int profile_id = 0; //Unique identifing code of user
AWSManager.Instance.DeleteAccountFolder(profile_id)
```

<?php
$validExtensions = array("zip"); //array("jpg", "jpeg", "txt"); //Set to null to ignore
$validMimeTypes = null; // array("application/x-zip-compressed"); //array("image/jpeg", "image/pjpeg", "text/plain"); //Set to null to ignore
$validAddresses = null; //array("127.0.0.1"); //Set to null to ignore
$validName = "/(\\w+\\.)\\w+/"; //Set to null to ignore
$safeUploadDir = "uploads/"; //Web server needs write permissions (maybe read as well?)
$logFilePath = "applogs/log.txt"; //Web server needs write permissions
$maxSize = 2; //MB (Integer)
$maxUploadTime = 120; //Seconds

// These define the details for the notification emails
$sendMailTo = null; // Email address to notify, null to not send email notifications
$sendMailFrom = null; // Email address to send from
$sendMailFromName = null; // Name to send email from
$sendMailReplyTo = null; // Reply to field for email (probably noreply@...)
$sendMailSubject = null; // Email subject

ini_set("upload_max_filesize", $maxSize."M");
ini_set("post_max_size", ($maxSize+1)."M");
ini_set("max_execution_time", $maxUploadTime);
ini_set("max_input_time", $maxUploadTime);
ini_set("max_file_uploads", 1);

if($validAddresses != null && !in_array($_SERVER["REMOTE_ADDR"], $validAddresses))
{
    LogError("Security error, unexpected IP address", false);
    exit;
}

function LogError($error, $returnError = true)
{
    global $logFilePath;
    $ipAddress = $_SERVER["REMOTE_ADDR"];
    $dateTime = date(DATE_RFC822);
    $msg = "$error - $dateTime - $ipAddress".PHP_EOL;
    $logFileHandle = fopen($logFilePath, "a");
    fwrite($logFileHandle, $msg);
    fclose($logFileHandle);
	if ($returnError) header("HTTP/1.1 500 Internal Server Error", true, 500);
}

function mail_attachment($filename, $path, $mailto, $from_mail, $from_name, $replyto, $subject, $message) {
    $file = $path.$filename;
    $file_size = filesize($file);
    $handle = fopen($file, "r");
    $content = fread($handle, $file_size);
    fclose($handle);
    $content = chunk_split(base64_encode($content));
    $uid = md5(uniqid(time()));
    $name = basename($file);
    $header = "From: ".$from_name." <".$from_mail.">\n";
    $header .= "Reply-To: ".$replyto."\n";
    $header .= "MIME-Version: 1.0\n";
    $header .= "Content-Type: multipart/mixed; boundary=\"".$uid."\"\n\n";
    $header .= "This is a multi-part message in MIME format.\n";
    $header .= "--".$uid."\n";
    $header .= "Content-type:text/plain; charset=iso-8859-1\n";
    $header .= "Content-Transfer-Encoding: 7bit\n\n";
    $header .= $message."\n\n";
    $header .= "--".$uid."\n";
    $header .= "Content-Type: application/octet-stream; name=\"".$filename."\"\n"; // use different content types here
    $header .= "Content-Transfer-Encoding: base64\n";
    $header .= "Content-Disposition: attachment; filename=\"".$filename."\"\n\n";
    $header .= $content."\n\n";
    $header .= "--".$uid."--";
    if (!mail($mailto, $subject, "", $header)) {
        LogError("Couldn't send mail");
    }
}

if(!array_key_exists("file", $_FILES))
{
    LogError("No file uploaded");
    exit;
}

$error = $_FILES["file"]["error"];
if($error != UPLOAD_ERR_OK)
{
    if($error == UPLOAD_ERR_INI_SIZE)
    {
        LogError("The uploaded file exceeds the upload_max_filesize directive in php.ini");
    }
    elseif($error == UPLOAD_ERR_FORM_SIZE)
    {
        LogError("The uploaded file exceeds the MAX_FILE_SIZE directive that was specified in the HTML form");
    }
    elseif($error == UPLOAD_ERR_PARTIAL)
    {
        LogError("The uploaded file was only partially uploaded");
    }
    elseif($error == UPLOAD_ERR_NO_FILE)
    {
        LogError("No file was uploaded");
    }
    elseif($error == 5)
    {
        LogError("File uploaded was empty (This error should not occur!)");
    }
    elseif($error == UPLOAD_ERR_NO_TMP_DIR)
    {
        LogError("Missing a temporary folder");
    }
    elseif($error == UPLOAD_ERR_CANT_WRITE)
    {
        LogError("Failed to write file to disk");
    }
    elseif($error == UPLOAD_ERR_EXTENSION)
    {
        LogError("A PHP extension stopped the file upload");
    }
    else
    {
        LogError("Upload error: Code ".$error);
    }
   
    if($error != 5)
    {
        exit;
    }
}

$filename = $_FILES["file"]["name"];
$mimeType = $_FILES["file"]["type"];
$size = $_FILES["file"]["size"]; //Bytes
$sizeInMB = intval($size / 1024 / 1024);
$tempLocation = $_FILES["file"]["tmp_name"];
$extension = end(explode(".", $filename));

if($validName != null && preg_match($validName, $filename) == 0)
{
    LogError("Filename '$filename' did not validate");
    exit;
}
if($validExtensions != null && !in_array($extension, $validExtensions))
{
    LogError("Extension '$extension' not acceptable (File $filename)");
    exit;
}
if($validMimeTypes != null && !in_array($mimeType, $validMimeTypes))
{
    LogError("Mime type '$mimeType' not acceptable (File $filename)");
    exit;
}
if($sizeInMB > $maxSize)
{
    LogError("File is too large: ".$sizeInMB."MB > ".$maxSize."MB  (File $filename)");
    exit;
}

$currentDate = date_create();
$counter = 1;
$newname = $currentDate->format("Y-m-d-H-i-s")."_".$_SERVER['REMOTE_ADDR']."_".$counter.".".end(explode(".", $filename));
while (file_exists($safeUploadDir.$newname)) {
	$counter++;
	$newname = $currentDate->format("Y-m-d-H-i-s")."_".$_SERVER['REMOTE_ADDR']."_".$counter.".".end(explode(".", $filename));
}
$filename = $newname;

if(move_uploaded_file($tempLocation, $safeUploadDir.$filename))
{
    LogError("Successfully uploaded $filename", false);
	if ($sendMailTo != null) {
		mail_attachment($filename, $safeUploadDir, $sendMailTo, $sendMailFrom, $sendMailFromName, $sendMailReplyTo,$sendMailSubject, $filename);
	}
}
else
{
    LogError("Could not move $filename to $safeUploadDir");
    exit;
}
?>
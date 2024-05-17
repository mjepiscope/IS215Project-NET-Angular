# **Automatic Facial Analysis from Images using Amazon Rekognition**

### **Project Overview**
 
This project, Automatic Facial Analysis from Images using Amazon Rekognition, demonstrates how to use AWS services for image analysis and automatic news generation using OpenAI's ChatGPT.

### **Key Features**
  1. Image Upload: Users can upload images via a web interface.
  2. Image Storage: Uploaded images are stored in an AWS S3 bucket.
  3. Serverless Computing: AWS Lambda functions are triggered on image upload to S3, analyzing the images using Amazon Rekognition.
  4. Facial Analysis: AWS Rekognition performs facial detection and analysis, outputting detailed facial features in JSON format.
  5. Automatic News Generation: Using the Rekognition output, ChatGPT generates a news article about the individual(s) in the image.
  6. Web Presentation: The generated news article is displayed on a web page.

![image](https://github.com/mjepiscope/IS215Project-NET-Angular/assets/13115232/45ccc3eb-0292-410d-9355-57e969c89203)

## **Project Flow**

  1. Image Upload
       - Users upload an image through a web interface hosted on an AWS EC2 instance.
       - The image is stored in an AWS S3 bucket.
  2. Trigger Lambda Function:
       - An S3 PUT event triggers an AWS Lambda function.
       - The Lambda function processes the image using AWS Rekognition, which returns detailed facial analysis in JSON format.
       - The detailed facial analysis is stored in DynamoDB which can be viewed at the page.
  3. Generate News Article:
       - The Lambda function uses the Rekognition output to form a prompt for ChatGPT.
       - ChatGPT generates a fictional news article based on the facial attributes provided by Rekognition.
       - The generated article is stored in DynamoDB.
  4. Display Article:
       - The generated news article is stored in DynamoDB.
       - The article is retrieved and displayed on a web page.
         
## **Detailed Flowchart**

![image](https://github.com/mjepiscope/IS215Project-NET-Angular/assets/13115232/74d8531d-8a37-4add-bd1b-12e3f90365b4)

## **Usage**
	1. Visit the web application URL.
	2. Upload an image via the provided interface.
	3. Wait for the Lambda function to process the image.
	4. View the generated news article on the web page.
   

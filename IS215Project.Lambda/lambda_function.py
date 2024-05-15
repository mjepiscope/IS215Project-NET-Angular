import boto3
import os
import requests
import json

# Get Item By Filename from Dynamo DB - Article
from boto3.dynamodb.conditions import Attr

# Initialize AWS clients
s3_client = boto3.client('s3')
rekognition_client = boto3.client('rekognition')
dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('Article')

# Get API key from environment variables
api_key = os.environ.get('IS215_API_KEY')
api_key = "Bearer " + api_key if api_key else None
#output_bucket_name = "test-is215-output"


def lambda_handler(event, context):
    headers = {
        'Content-Type': 'application/json',
        'Authorization': api_key
    }

    try:
        # Get the bucket name and object key from the S3 event
        records = event['Records']
        bucket_name = records[0]['s3']['bucket']['name']
        object_key = records[0]['s3']['object']['key']

    except KeyError as e:
        print(f"KeyError: {e} - The expected key is not present in the event.")
        return {
            'statusCode': 400,
            'body': f"KeyError: {e} - The expected key is not present in the event."
        }
    except IndexError as e:
        print(f"IndexError: {e} - The expected index is out of range in the event.")
        return {
            'statusCode': 400,
            'body': f"IndexError: {e} - The expected index is out of range in the event."
        }

    # Filenames for output folder
    filename = os.path.splitext(object_key)[0]+".txt"
    #filename_title = os.path.splitext(object_key)[0]+"_title.txt"
    #filename_rekognition = os.path.splitext(object_key)[0]+"_rekognition.txt"
    #temp_file_path = '/tmp/{}'.format(os.path.basename(object_key))

    
    # Perform facial detection and analysis using Rekognition
    try:
        response_rekognition = rekognition_client.detect_faces(
            Image={'S3Object':{'Bucket':bucket_name,'Name':object_key}},
            Attributes=['ALL']
        )
        
        # Convert to string to save to db
        rekognition_response_str = json.dumps(response_rekognition)
        
        scanResponse = table.scan(FilterExpression=Attr('ImageFilename').eq(object_key))
        items = scanResponse['Items']
        article = items[0]

        # Update Dynamo DB - Article Item with Rekognition Response
        updateResponse = table.update_item(
            Key={"Timestamp": article['Timestamp']},
            UpdateExpression="set RekognitionResponse=:r",
            ExpressionAttributeValues={
                ":r": rekognition_response_str
            }
        )

    except Exception as e:
        print(f"Error processing image with Rekognition: {e}")
        return {
            'statusCode': 500,
            'body': 'Error processing image with Rekognition.'
        }

    # Process the Rekognition response_rekognition
    if 'FaceDetails' in response_rekognition:
        num_faces = len(response_rekognition['FaceDetails'])
        #print(f"Detected {num_faces} face(s) in the uploaded image.")

        # Extract facial attributes and perform further analysis as needed
        for face in response_rekognition['FaceDetails']:
            # Extract facial attributes (e.g., age, gender, emotions)
            age_range_low = face['AgeRange']['Low']
            age_range_high = face['AgeRange']['High']
            gender = face['Gender']['Value']
            emotions = face['Emotions'][0]['Type']

        age_range = (age_range_low + age_range_high) // 2
        print("Age: ", age_range)
        print("Gender: ", gender)
        print("Emotions: ", emotions)

        prompt = f"Can you generate a news article based on the age is {age_range}, the gender is {gender}. and emotion is {emotions}."

        payload = {
            "model": "gpt-3.5-turbo",
            "messages": [
                {
                    "role": "user",
                    "content": prompt,
                    "temperature": 0.7,
                    "max_tokens": 600
                }
            ]
        }

        # Send the request to the OpenAI API endpoint
        try:

            response_AI = requests.post("https://is215-openai.fics.store/v1/chat/completions", headers=headers, json=payload)
            print("Response from requests.post call: ", response_AI)

            if response_AI.status_code == 200:
                # Extract the generated text from the response_AI
                response_json = response_AI.json()
                generated_text = response_json['choices'][0]['message']['content']
                split_text = generated_text.split('\n', 1)
                title = split_text[0]
                generated_text = split_text[1].strip() if len(split_text) > 1 else ""
                
                print("Response from OpenAI API:", generated_text)
                #encoded_article = generated_text.encode("utf-8")
                
                # Update Dynamo DB - Article Item with Article Title and Generated Content
                updateResponse = table.update_item(
                    Key={"Timestamp": article['Timestamp']},
                    UpdateExpression="set ArticleTitle=:t, GeneratedContent=:c",
                    ExpressionAttributeValues={
                        ":t": title,
                        ":c": generated_text
                    }
                )

                return {
                    'statusCode': 200,
                    'body': json.dumps({
                        'message': 'Successfully processed the image and generated the article.',
                        'body': generated_text
                    })
                }
            else:
                print("Error: Failed to generate text")
                return {
                    'statusCode': response.status_code,
                    'body': 'Error: Failed to generate text'
                }

        except Exception as e:
            print(f"Error calling OpenAI API: {e}")
            return {
                'statusCode': 500,
                'body': f"Error calling OpenAI API: {e}"
            }
    else:
        print("No faces detected in the uploaded image.")  # Print statement for testing
        return {
            'statusCode': 400,
            'body': 'No faces detected in the uploaded image.'
        }

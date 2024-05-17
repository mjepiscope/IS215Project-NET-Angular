import boto3
import os
import requests
import json
import re
import time
from boto3.dynamodb.conditions import Key

rekognition_client = boto3.client('rekognition')
dynamodb = boto3.resource('dynamodb')
table = dynamodb.Table('Article')

api_key = os.environ.get('IS215_API_KEY')
api_key = "Bearer " + api_key

def get_timestamp_from_dynamodb(timestamp):
    try:
        timestamp = int(timestamp)
        response = table.query(
            KeyConditionExpression=Key('Timestamp').eq(timestamp)
        )
        items = response.get('Items', [])
        if items:
            return items[0]
    except Exception as e:
        print(f"Error querying DynamoDB: {e}")
    return None

def call_openai_api(payload, headers):
    retry_attempts = 3
    for attempt in range(retry_attempts):
        try:
            response = requests.post(
                "https://is215-openai.fics.store/v1/chat/completions", 
                headers=headers, 
                json=payload,
                timeout=10  # Setting a timeout of 10 seconds
            )
            if response.status_code == 200:
                return response.json()
            else:
                print(f"Attempt {attempt+1}: Error: Failed to generate text with status code {response.status_code}")
        except requests.exceptions.Timeout:
            print(f"Attempt {attempt+1}: Timeout occurred while calling OpenAI API")
        except Exception as e:
            print(f"Attempt {attempt+1}: Error calling OpenAI API: {e}")
        time.sleep(2 ** attempt)  # Exponential backoff
    return None

def lambda_handler(event, context):
    headers = {
        'Content-Type': 'application/json',
        'Authorization': api_key
    }

    try:
        records = event['Records']
        bucket_name = records[0]['s3']['bucket']['name']
        object_key = records[0]['s3']['object']['key']
        
        pattern = r"\.(\d+)\."
        match = re.search(pattern, object_key)
        timestamp = match.group(1)
        
        print(timestamp)
    except KeyError as e:
        print(f"KeyError: {e} - The expected key is not present in the event.")
        return {'statusCode': 400, 'body': f"KeyError: {e} - The expected key is not present in the event."}
    except IndexError as e:
        print(f"IndexError: {e} - The expected index is out of range in the event.")
        return {'statusCode': 400, 'body': f"IndexError: {e} - The expected index is out of range in the event."}

    try:
        response_rekognition = rekognition_client.detect_faces(
            Image={'S3Object': {'Bucket': bucket_name, 'Name': object_key}},
            Attributes=['ALL']
        )
        rekognition_response_str = json.dumps(response_rekognition)
    except Exception as e:
        print(f"Error processing image with Rekognition: {e}")
        return {'statusCode': 500, 'body': 'Error processing image with Rekognition.'}

    if 'FaceDetails' in response_rekognition:
        num_faces = len(response_rekognition['FaceDetails'])
        print(f"Detected {num_faces} face(s) in the uploaded image.")

        for face in response_rekognition['FaceDetails']:
            age_range_low = face['AgeRange']['Low']
            age_range_high = face['AgeRange']['High']
            gender = face['Gender']['Value']
            emotions = face['Emotions'][0]['Type']

        age_range = (age_range_low + age_range_high) // 2
        print("Age: ", age_range)
        print("Gender: ", gender)
        print("Emotions: ", emotions)

        prompt = f"Can you generate a news article with title that has {num_faces} people but focus on 1 based on the age as {age_range}, gender as {gender}, and emotion as {emotions}."

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

        response_json = call_openai_api(payload, headers)
        print(response_json)
        if response_json:
            generated_text = response_json['choices'][0]['message']['content']
            split_text = generated_text.split('\n', 1)
            title = split_text[0]
            generated_text = split_text[1].strip() if len(split_text) > 1 else ""
                
            item = get_timestamp_from_dynamodb(timestamp)
            if item:
                table.update_item(
                    Key={
                        "Timestamp": int(timestamp)
                        #"ArticleID": item['ArticleID'] 
                    },
                    UpdateExpression="set ArticleTitle=:t, GeneratedContent=:c, RekognitionResponse=:r",
                    ExpressionAttributeValues={
                        ":t": title,
                        ":c": generated_text,
                        ":r": rekognition_response_str
                    }
                )
            return {'statusCode': 200, 'body': json.dumps({'message': 'Successfully processed the image and generated text.', 'body': generated_text})}
        else:
            return {'statusCode': 500, 'body': 'Error: Failed to generate OpenAI text'}
    else:
        print("No faces detected in the uploaded image.")
        return {'statusCode': 400, 'body': 'No faces detected in the uploaded image.'}

import boto3
import os
import requests
import json

s3_output = boto3.client('s3')
s3_client = boto3.client('s3')
rekognition_client = boto3.client('rekognition')
api_key = os.environ.get('IS215_API_KEY')
api_key = "Bearer " + api_key
output_bucket_name = "test-is215-output"

def lambda_handler(event, context):
    # OpenAPI setup
    # Define the headers and payload
    headers = {
        'Content-Type': 'application/json',
        'Authorization': api_key
    }

    #print("Event received:", json.dumps(event, indent=2))  # Log the event payload
    #print("Lambda function execution started.")  # Print statement for testing

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
    filename_title = os.path.splitext(object_key)[0]+"_title.txt"
    filename_rekognition = os.path.splitext(object_key)[0]+"_rekognition.txt"
    #temp_file_path = '/tmp/{}'.format(os.path.basename(object_key))

    # Download the image file from S3
    #try:
    #    s3_client.download_file(bucket_name, object_key, temp_file_path)
    #except Exception as e:
    #    print(f"Error downloading file from S3: {e}")
    #    # Add generated text for error output
    #    error_downloading = """There's an error processing your uploaded image. Can you please try uploading again?
    #    Make sure to note this project only supports these image extensions (.jpg, .jpeg (JPEG), .png (Portable Network Graphics), .bmp (Bitmap), and .tiff, .tif (Tagged Image File Format))"""
    #    s3_output.put_object(
    #        Body=error_downloading,
    #        Bucket=output_bucket_name,
    #        Key=filename
    #    )
    #    return {
    #        'statusCode': 500,
    #        'body': 'Error downloading file from S3.'
    #    }

    # Perform facial detection and analysis using Rekognition
    try:
        # with open(temp_file_path, 'rb') as image_file:
        #     response = rekognition_client.detect_faces(
        #         Image={'Bytes': image_file.read()},
        #         Attributes=['ALL']
        #     )

        # Pass S3 Image File directly to Rekognition
        response = rekognition_client.detect_faces(
            Image={'S3Object':{'Bucket':bucket_name,'Name':object_key}},
            Attributes=['ALL']
        )

        #print(response)
    except Exception as e:
        print(f"Error processing image with Rekognition: {e}")
        # Add generated text for error output
        error_rekognition = "The uploaded image cannot be processed using the Amazon Rekognition. Please try with a different image."
        s3_output.put_object(
            Body=error_rekognition,
            Bucket=output_bucket_name,
            Key=filename
        )
        return {
            'statusCode': 500,
            'body': 'Error processing image with Rekognition.'
        }

        # Add the Amazon Rekognition result to output bucket
        s3_output.put_object(
            Body=response,
            Bucket=output_bucket_name,
            Key=filename_rekognition
        )

    
    # Get Item By Filename from Dynamo DB - Article
    from boto3.dynamodb.conditions import Attr
    
    dynamodb = boto3.resource('dynamodb')
    table = dynamodb.Table('Article')
    
    scanResponse = table.scan(
        FilterExpression=Attr('ImageFilename').eq(object_key)
    )
    
    # TODO Add Validation if Item/Article is not found in DynamoDB

    items = scanResponse['Items']
    article = items[0]
    #print(article)

    # Update Dynamo DB - Article Item with Rekognition Response and OpenAI Generated Content
    # TODO Do another update after getting OpenAI Response
    updateResponse = table.update_item(
        Key={"Timestamp": article['Timestamp']},
        UpdateExpression="set RekognitionResponse=:r, GeneratedContent=:c",
        ExpressionAttributeValues={
            ":r": str(response),
            ":c": "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Pellentesque placerat nunc nec leo finibus, at porta sapien commodo. Morbi dictum ante velit, quis fringilla urna finibus nec. Ut consectetur congue purus at feugiat. Nulla sed scelerisque elit, quis sagittis massa. Aenean risus turpis, tempor at velit nec, porttitor consectetur est. Mauris sed quam in lectus tempor venenatis. Nam suscipit accumsan ipsum ut ornare. Nunc commodo dui at nisl efficitur interdum. Quisque id tellus ullamcorper, feugiat arcu in, accumsan mauris. Phasellus risus metus, venenatis fringilla velit volutpat, porta lacinia enim. Suspendisse eget lectus ac turpis feugiat lobortis. Ut pulvinar eu purus nec pharetra. Etiam turpis turpis, finibus non tellus eu, molestie consequat ipsum."
        }
    )
    #print(updateResponse)
        
    # Process the Rekognition response
    if 'FaceDetails' in response:
        num_faces = len(response['FaceDetails'])
        print(f"Detected {num_faces} face(s) in the uploaded image.")  # Print statement for testing

        # Extract facial attributes and perform further analysis as needed
        for face in response['FaceDetails']:
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
                #print(response_json)
                generated_text = response_json['choices'][0]['message']['content']
                split_text = generated_text.split('\n', 1)
                title = split_text[0]
                generated_text = split_text[1].strip() if len(split_text) > 1 else ""
                print("Response from OpenAI API:", generated_text)
                encoded_article = generated_text.encode("utf-8")

                # Put title of the content
                s3_output.put_object(
                    Body=title,
                    Bucket=output_bucket_name,
                    Key=filename_title
                )

                # Put generated content
                s3_output.put_object(
                    Body=encoded_article,
                    Bucket=output_bucket_name,
                    Key=filename
                )

                return {
                    'statusCode': 200,
                    'body': json.dumps({
                        'message': 'Successfully processed the image and generated text.',
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

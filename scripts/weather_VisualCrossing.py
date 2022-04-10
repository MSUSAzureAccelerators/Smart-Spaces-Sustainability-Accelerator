''' This script will make a request to the Visual Crossing api, it will be passed a from date from the data factory pipeline '''

import requests
import json
import os
from azure.storage.blob import BlobServiceClient, BlobClient, ContainerClient
from azure.appconfiguration import AzureAppConfigurationClient
from azure.keyvault.secrets import SecretClient
from azure.identity import DefaultAzureCredential, ManagedIdentityCredential
import sys
import re
import ast
import time
from datetime import datetime, timedelta, date, timezone
from urllib import parse

def retrieve_blob_client(connect_str, container, blob):
    ''' takes an azure storage connection string, container
    and returns a client connected to the blob'''

    blob_service_client = BlobServiceClient.from_connection_string(connect_str)

    blob_client = blob_service_client.get_blob_client(container=container, blob=blob)

    return blob_client

def limit_to_40_days_ago(from_date=None):
    '''accepts a date and returns that date or 60 days ago from now, whichever is less. returns 20 days ago if no date given'''
    return_date = datetime.utcnow().date() - timedelta(days=40)
    
    if not from_date is None:
        if from_date > return_date:
            return_date = from_date
    
    return return_date


def retrieve_timestamp(blob_client, ts_file):
    '''takes a blob client and returns the time stamp stored within as a string'''
    with open(ts_file, 'wb') as my_blob:
        blob_data = blob_client.download_blob()
        blob_data.readinto(my_blob)

    with open(ts_file, 'r') as ts:
        x = ast.literal_eval(re.search('({.+})', ts.read()).group(0))

    ts_dict = dict(x)

    return ts_dict

def pull_weather_data(from_date, location, key):
    '''makes a request to Visual Crossing api and returns a json object'''
    
    base_url = "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline"
    location = parse.quote(location)
    from_date_str = str(from_date)
    to_date_str = str(datetime.utcnow().date())
    full_url = "/".join([base_url, location, from_date_str, to_date_str])
    
    params = { 
        "key": key,
        "unitGroup": "us",
        "elements": "datetimeEpoch,datetime,windspeed,temp,humidity,windgust,solarradiation",
        "include": "hours,obs",
        "contentType": "json"
    }

    response = requests.get(full_url, params=params)
    
    result = response.json()

    return result


def transform_result(result, datetime_format):
    new_result = {'records':[],'station':{}}
    result_tz = result['tzoffset']
    
    # Insert header row
    new_result['records'].append(["Timestamp", "10 Minute Wind Gust", "Anemometer", "Hygrometer", "Solar Radiation Sensor", "Thermometer"])
    
    for day in result['days']:
        for hour in day['hours']:
            # Account for daylight savings change
            if 'tzoffset' in hour:
                hour_tz = hour['tzoffset']
            else:
                hour_tz = result_tz
            
            row_datetime = datetime.fromtimestamp(hour['datetimeEpoch'], tz=timezone(timedelta(hours=hour_tz)))
            
            row = [
                row_datetime.strftime(datetime_format),
                hour['windspeed'] or 0.0,
                hour['windgust'] or 0.0,
                hour['solarradiation'] or 0.0,
                hour['temp'] or 0.0
            ]
            
            # Check if this is a null row
            if sum(row[1:]) == 0.0:
                continue
            
            new_result['records'].append(row)
    
    return new_result


def write_result_to_file(result, file_name):
    with open(file_name, 'w') as outfile:
        json.dump(result, outfile)
    return True


def write_weather_to_blob(blob_client, weather_file):
    '''takes a weatherSTEM json response and writes it as a txt file to azure blob storage'''
  
    with open(weather_file, "rb") as data:
        blob_client.upload_blob(data, overwrite=True)

    return True


if __name__ == "__main__":

    input = str(sys.argv[1])

    #Retrieve connection string from app config stored in extended paramaters of adf
    with open('activity.json', 'r') as params:
        data = json.load(params)

    kv_uri = data['typeProperties']['extendedProperties']['kvURI']

    credential = ManagedIdentityCredential()

    client = SecretClient(vault_url=kv_uri, credential=credential)
    
    location = data['typeProperties']['extendedProperties']['location']
    
    api_key = str(client.get_secret('visualCrossingAPIKey'))

    connect_str = str(client.get_secret('storageConnectString'))
    
    container = 'weather'

    weather_file = 'weather.txt'

    input_datetime_format = '%Y-%m-%dT%H:%M:%S'
    
    output_datetime_format = '%Y-%m-%d %H:%M:%S'

    timestamp = 'timestamp.txt'

    if input == "since_last":

        #retrieve blob client for last timestamp
        blob_client = retrieve_blob_client(connect_str, container, timestamp)

        #retrieve last timestamp from last_Ts text file
        from_datetime_str = retrieve_timestamp(blob_client, timestamp)['last_time']
        
        #convert to date
        from_date = datetime.strptime(from_datetime_str, input_datetime_format).date()
        from_date = limit_to_40_days_ago(from_date)

        #retrieve weather data
        weather_data = pull_weather_data(from_date, location, api_key)
        
        #transform data
        weather_data = transform_result(weather_data, output_datetime_format)
        
        #save data to local
        write_result_to_file(weather_data, weather_file)

        #retrieve blob client for weather.txt
        blob_client = retrieve_blob_client(connect_str, container, weather_file)

        #write Visual Crossing response to blob client
        write_weather_to_blob(blob_client, weather_file)
    
    elif input == "full_history":

        #pulls 40 days of data
        from_date = limit_to_40_days_ago()

        #retrieve weather data
        weather_data = pull_weather_data(from_date, location, api_key)
        
        #transform data
        weather_data = transform_result(weather_data, output_datetime_format)
        
        #save data to local
        write_result_to_file(weather_data, weather_file)

        #retrieve blob client for weather.txt
        blob_client = retrieve_blob_client(connect_str, container, weather_file)

        #write Visual Crossing response to blob client
        write_weather_to_blob(blob_client, weather_file)

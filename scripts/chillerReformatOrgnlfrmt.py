from azure.storage.blob import BlobServiceClient, BlobClient, ContainerClient
from azure.appconfiguration import AzureAppConfigurationClient
import json
import os
import sys

def retrieve_blob_client(connect_str, container, blob):
    ''' takes an azure storage connection string, container
    and returns a client connected to the blob'''

    blob_service_client = BlobServiceClient.from_connection_string(connect_str)

    blob_client = blob_service_client.get_blob_client(container=container, blob=blob)

    return blob_client

if __name__ == "__main__":

    #Retrieve connection string from app config stored in extended paramaters of adf
    with open('activity.json', 'r') as params:
        data = json.load(params)

    app_config_conn_str = data['typeProperties']['extendedProperties']['appConfig']

    client = AzureAppConfigurationClient.from_connection_string(app_config_conn_str)

    azure_storage_connect_str = client.get_configuration_setting(
        key="AZURE_STORAGE_CONNECTION_STRING"
    )
   
    connect_str = azure_storage_connect_str.value

    container = 'extracts-clean'

    container_client = ContainerClient.from_connection_string(
       connect_str, container_name=container)

    #Retrieve list of files with given file pattern from extract-clean container
    blobs_list = container_client.list_blobs(name_starts_with = "Chiller_Summary_Clean")
    
    #handle each file individually
    for blob in blobs_list:
        container = 'extracts-clean' 
        blob_client = retrieve_blob_client(connect_str, container, blob.name)

        with open(blob.name, 'wb') as my_blob:
            blob_data = blob_client.download_blob()
            blob_data.readinto(my_blob)

        with open(blob.name, 'r') as chillerData:
            data = json.load(chillerData)

        if isinstance(data['ChillerDataObj'], list):
            continue
        else:
            #update 'ChillerDataObj' to just be values of all assorted keys ie list of dictionaries
            with open(blob.name, 'w') as chillerData:
                data['ChillerDataObj'] = list(data['ChillerDataObj'].values())
                json.dump(data, chillerData)

            #rewrite files to the blob for data factory copy to sql
            with open(blob.name, "rb") as data:
                blob_client.upload_blob(data, overwrite=True)




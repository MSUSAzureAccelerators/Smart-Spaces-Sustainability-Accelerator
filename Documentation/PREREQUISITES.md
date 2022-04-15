# Steps to take before Deploying

1. Retrieve Azure User ObjectID.
    1. Within Azure resource group, open azure cli
    2. enter command: Get-AzADUser
    3. Copy the Object ID associated with the desired user to request the deployment.

2. Retrieve Visual Crossing API Key
    1. Register for a FREE account.
    2. Navigate to user's Account
    3. Copy the Key associated with the account.

3. A Location meeting the requirements of a Visual Crossing request
    1. One or more address, partial address or latitude, longitude values for the required locations . Addresses can be specified as full addresses. The system will also attempt to match partial addresses such as city, state, zip code, postal code and other common formats.

    2. When specify a point based on longitude, latitude, the format must be specified as latitude,longitude where both latitude and longitude are in decimal degrees. latitude should run from -90 to 90 and longitude from -180 to 180 (with 0 being at the prime meridian through London, UK).

    3. Data for multiple locations can be requested in a single request by concatenating multiple locations using the pipe (|) character.

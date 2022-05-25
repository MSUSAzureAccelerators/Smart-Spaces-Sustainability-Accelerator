Architecture of how resources authenticate with each other.

1. #### System Assigned Managed Identities:
    - The Azure Data Factory (ADF) is created as the first resource so that its system assigned managed identity can be granted to most other resources on creation, by assigning "Contributor" access rights to the ADF SAMI.
    - Additionally, the IOTHub resource is also configured with a User Assigned Managed Identity as part of the SAMI interface.. 

2. #### User Assigned Managed Identities:
   ##### Azure Data Factory:
    - The User Assigned Managed Identity is created for two resources that do not have support for system assigned managed idenities.
    - The first is the deploy scripts task.  It does not allow the ADF SAI to access azure CLI and deploy the scripts to blob storage so a user assigned managed idenity is created and assigned the role to deploy files to blob
    - The second is for accessing a specific batch pool within a batch account.
        - The ADF SAI grants access to the batch account, but a pool cannot give access to a SAI so a user assigned managed identity is needed.
    ##### Azure IOTHub:
    - The IOTHub is also configured with a User Assigned Managed Identity.
    - This provisioning in the 2nd step of the deployment process - via the deployment to Azure IOTHub.
    - One of the primary reasons for the Sytam-assigned managed identity is so that we can apply other reources during the provisionng process. 
    - For example, in this step, there is a need to run an "in-line" Powershell script in order to provision (4) IotHub Devices.
    - 
    -  

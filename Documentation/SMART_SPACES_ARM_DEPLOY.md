#High Level Description of Resources Included in Deployment and Structure of Templates

The Structure of the ARM templates makes use of a master template that is deployed via the azure deploy button, and linked templates called from the master template.  This repository is public, so each linked template reference is to a specific file in the associated branch.

Summary of Resources Deployed:

1. DataFactory V2 with a System Assigned Identity.
2. Azure Blob Storage, with the following containers: ["adfjobs",
        "log",
        "datascience",
        "exctracts-clean-archive",
        "extracts-clean",
        "extracts-raw",
        "scripts",
        "weather"]
    1. System assigned identity, and contributor role assignment for the DataFactory (ADF) System Assigned Idenity (SAI)
3. User Assigned Managed Idenity (Used for resources where SAI was not supported)
4. Secret Vault with Access Policies for the objectID used to deploy the template, the UserAssigned Identity and the ADF SAI
    1. Secrets created for SQL Password, Blob Storage Access Key, SQL Connection String, Storage Connection String and a Visual Crossing API Key.
5. A Batch Account with one dedicated standard_d11 VM node.
    1. The Image used is a data science specific windows image.
    2. the start task installs the neccesary python packages to be used by the different data factory processes.
    3. Access granted to ADF SAI
    4. Pool Access granted to User Assigned Identity (Not able to grant to System Assigned Identity)
6. Access to Storage given to User Assigned Managed Identity
7. Python Scripts and SQL BACPAC are deployed to scripts container of the blob storage using the user assigned managed identity.
8. SQL Server with Password stored in azure key vault, adminusername set to testAdmin, and database deployed from BACPAC file stored in blob storage.
9. azureML model is deployed and registered, equipt with key vault encryption.
10. ADF Properties
    1. linked services created for blob storage, batch account, sql and key vault.
    2. datasets for all sql schemas.
    3. dataflows to move all IOT data to respective final SQL tables for powerBI consumption.
    4. pipelines to orchestrate dataflows, run Visual crossing weather pulls, and run hvac and weather forecasting scripts.
    5. Custom triggers.
        - one trigger to do a one time 2 month weather pull from visual crossing.
        - one trigger to do the hourly weather pull starting 24 hours after 2 month pull (to account for visual crossing daily limit)
        - one trigger to start hourly pulls from iot intermediate tables.
        - one trigger to do nightly forecast for hvac and weather, starting at 0300 the day after deployment.




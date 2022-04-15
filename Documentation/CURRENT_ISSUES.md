1. The Batch Account Quotas imposed on new batch accounts seems to require a service request to increase the batch vm quota.

4/14 Update: Possible work around by making use of User Subscription pool allocation.  This means that the VMS are deployed directly to the subscription.  

https://docs.microsoft.com/en-us/azure/batch/batch-account-create-portal


2. not all regions currently allow sql database provisioning.
    - Current work arround is to hard code deployment to 'west us' within the templates/sql/linked_sql_server_db.json


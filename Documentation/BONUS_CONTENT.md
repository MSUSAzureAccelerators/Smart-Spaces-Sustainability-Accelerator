# Bonus Content

#### NOTE: This accelerator contains (3) additional Azure resources to help you effectively manage your Azure cloud spend.

Specifically, the (3) Azure resources consist of the following Items: 

#### (1) Azure Function App  
     * FuncCreateJWTTOKEN...

#### (2) Logic Apps:

        * LogicApp-ASA-START-...

        * LogicApp-ASA-STOP-...

These additional Azure resources will allow you to control exactly when and for how long - the Azure Stream Analytics service is running.  

Best practice guidance would be to run the START Logic App every Hour. 
Then trigger the STOP Logic App to run every Hour - BUT FIVE MINUTES After the START Logic App has been triggered.
 



### Getting Started
Follow the steps below to complete the configuration for these additional Azure resources:

(1) Navigate to one of the the Logic Apps highlighted above.

(2) Select the option to EDIT the Logic App.

(3) DOUBLE-CICK the middle activity named "FuncCreateJWTToken".

(4) This will open-up the Logic App editor and reveal the JSON payload body as depicted below:

        {
          "applicationid": "<Your App ID>",
          "clientsecret": "<Your Client Secret>",
          "resource": "https://management.azure.com",
          "tenantid": "<Your Tenant ID>"
        }

(5) To retrieve the values required to populate the JSON payload above, we will run a short PowerShell script called "CREATE Svc Principal.txt".
    This file is located in the SCRIPTS folder of this repository. 

- Navigate to the Azure portal at https://portal.azure.com
 
- Open a CLOUD SHELL prompt at the top right-hand corner of the Azure Portal Web Page.

- Open and copy the script from https://raw.githubusercontent.com/MSUSSolutionAccelerators/Smart-Spaces-Sustainability-Solution-Accelerator/main/scripts/CREATE%20Svc%20Principal.txt into your favorite text editor.

- Modify the variables in the TOP portion of the script to match YOUR Azure Environment:

        $SvcPrinName = "acel-smart-spaces-svcprin"

        $RoleName="Contributor"

        $RGP = "Accel-Smart-SpacesRG"

- Then Select ALL and PASTE the modified script into the Azure Cloud Shell window.

- The Script should start to run automatically - and will display the values that you will need to construct the JSON payload above.

- If you need to Re-Display values:

- Simply type the variable names below into the CLOUD SHELL window - and the values will be re-displayed for you:
 
  #####  $SvcPrinAppId

  #####  $SvcPrinPwd

  #####  $TenantID

- Next, COPY the above values into the JSON payload below:

        {
          "applicationid": "<Your App ID>",
          "clientsecret": "<Your Client Secret>",
          "resource": "https://management.azure.com",
          "tenantid": "<Your Tenant ID>"
        }

- Then, PASTE this into the REQUEST Body of the FuncCreateJWTToken workflow step as depicted below:
![Logic App](https://raw.githubusercontent.com/MSUSSolutionAccelerators/Smart-Spaces-Sustainability-Solution-Accelerator/main/images/Logic_App_ASA_Start.png "Logic App")

- Click on SAVE to save the changes.

- You can click on RUN TRIGGER to test your changes and see that the Logic App completes successfully.   
 

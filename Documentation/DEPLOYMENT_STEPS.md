# Deployment Steps

#### Step 0 - Gather Pre-requisites:
Be sure to follow the pre-requisites guidance in the this document: [PREREQUISITES.md](https://github.com/MSUSSolutionAccelerators/Smart-Spaces-Sustainability-Solution-Accelerator/blob/featureIOTHubDeploy/Documentation/PREREQUISITES.md) 

#### Step 1 - Deploy "Back-end" Azure Resources:
This step entails the deployment of the Azure SQL database, an Azure  Data Factory for ingesting inputs, a Machine Learning batch processingenvironment, and reporting functionality.  Please follow these steps for 


[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FMSUSSolutionAccelerators%2FSmart-Spaces-Sustainability-Solution-Accelerator%2Ffeature%2FARMDeploy%2Ftemplates%2Fmaster_accelerator_deployment.json)


- Make sure that URL used in above button is URL Encoded.  This is the only raw git URL that needs to be encoded. 
- free encooding website https://www.urlencoder.org/



#### Step 2 - Deploy "Front-end" Azure IOTHub Simulator Resources:
This step entails the deployment of the IotHub "Simulator" resources; namely an IotHub with (4) devices, along with supporting Azure Functions, Logic Apps and an Azure Stream Anlytics job - which all work together to produce simulated temperature and HVAC cooling information readings for a representative "Smart Space".  

#### Note ####
Since this step relies on the Azure SQL Database that was provisioned in Step#1 - please make sure to review and confirm the Azure SQL database name and associated credentials are correctly specified in the #2 deployment script.

Please follow these steps for implementation: 


[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FMSUSSolutionAccelerators%2FSmart-Spaces-Sustainability-Solution-Accelerator%2Ffeature%2FARMDeploy%2Ftemplates%2Fmaster_accelerator_deployment.json)



- Navigate to [Azure Portal - Template Deployment](https://portal.azure.com/#create/Microsoft.Template)






#### Step 3 - Run a Script in the Cloud Shell in the Azure Portal:
This step entails navigating to the Azure Portal in a web browser, and then clicking on the "Cloud Shell" icon in the upper right-hand corner or the Azure Portal screen.

Below is an image of the "cloud Shell" in the navigation bar:

![Azure Portal Cloud Shell](https://raw.githubusercontent.com/MSUSSolutionAccelerators/Smart-Spaces-Sustainability-Solution-Accelerator/main/images/CloudShellPortal.png "[Azure Portal Cloud Shell")

Once you click on the "cloud Shell " icon, the screen will split - and you will see a 

Below is an image of the "cloud Shell" after it has been opened in the Azure Portal:

![Azure Portal Cloud Shell](https://raw.githubusercontent.com/MSUSSolutionAccelerators/Smart-Spaces-Sustainability-Solution-Accelerator/main/images/CloudShellPortal2.png "[Azure Portal Cloud Shell")

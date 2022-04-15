# Steps for adding Resources to the Deployment

1. Create or find a template that deploys your desired resource.
    - It often helps to check microsofts quick start templates
2. Alter the resources in the deployment to fit your use case.
3. If there are resources being deployed that already exist, pass the names or resource ids as parameters and reference them rather than creating duplicate resources.
4. Add the template to the master deployment template, referencing its location in the specific git branch to be deployed, and pass it required parameters.
5. In the "depends on" parameter of the linked deployment make sure to include the names of all deployments that must complete before that resource.
6. Make sure to commit and push changes to the remote branch before testing the deploy button.  
7. Keep in mind that the Deploy button is also coded to a specific file "master_accelerator_deployment.json" of a specific branch.
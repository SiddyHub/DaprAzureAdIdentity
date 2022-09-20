# Integration with AzureAd Identity

  Welcome to the Part 3 of the [Dapr Series](https://github.com/SiddyHub/Dapr/tree/eshop_daprized).
  
  This sample demonstrates our Front End ASP.NET Core Web App calling a Event Catalog ASP.NET Core Web API that is secured using Azure AD.

  The [main](https://github.com/SiddyHub/DaprAzureAdIdentity) branch, is the code base from [Part 2](https://github.com/SiddyHub/DaprDataManagement/tree/daprDataManagement) of the [Dapr Series](https://github.com/SiddyHub/Dapr/tree/eshop_daprized),
and [daprAzureAdIdentity](https://github.com/SiddyHub/DaprAzureAdIdentity/tree/daprAzureAdIdentity) branch is the refactored code base that uses the Microsoft identity platform to sign in users.

This version of the code uses **Dapr 1.7**

## Pre-Requisites to Run the Application

- VS Code
  - with [Dapr Extension](https://docs.dapr.io/developing-applications/ides/vscode/vscode-dapr-extension/)
  - and with [Azure Cache Extension](https://marketplace.visualstudio.com/items?itemName=ms-azurecache.vscode-azurecache)
- .NET Core 3.1 SDK
- Docker installed (e.g. Docker Desktop for Windows)
- [Dapr CLI installed](https://docs.dapr.io/getting-started/install-dapr-cli/)
- Access to an Azure subscription
- An **Azure AD** tenant. For more information, see: [How to get an Azure AD tenant](https://learn.microsoft.com/en-us/azure/active-directory/develop/test-setup-environment#get-a-test-tenant)
- A user account in your **Azure AD** tenant. This sample will not work with a **personal Microsoft account**. If you're signed in to the [Azure portal](https://portal.azure.com/) with a personal Microsoft account and have not created a user account in your directory before, you will need to create one before proceeding.

## Architecture Overview
   
   1. Our Front End client ASP.NET Core Web App uses the [Microsoft.Identity.Web](https://aka.ms/microsoft-identity-web) to sign-in a user and obtain a JWT Access Token from Azure AD.
   2. The access token is used as a bearer token to authorize the user to call the ASP.NET Core Web API protected by Azure AD.
   3. The service uses the [Microsoft.Identity.Web](https://aka.ms/microsoft-identity-web) to protect the Web api, check permissions and validate tokens.
      
   If need more information of our scenario, please do go through this [overview](https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-web-app-call-api-overview?view=aspnetcore-6.0).

## Setup the sample

   For our sample we would be following Part 1.

   - Part 1, `using Microsoft.Identity.Web`
     - Step 1:  Register the sample application(s) in your tenant
       
       Since we would be demonstrating our FrontEnd signing-in a user and calling an Event Catalog Web API that is secured with Azure AD, so for these two projects, each needs to be separately registered in your Azure AD tenant. 
       
       To Register our Front End Client project, follow the steps as mentioned in [this quickstart](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app#register-an-application).
       
       *When registering Frontend project set the Redirect URI as https://localhost:5000/signin-oidc and for Front-channel logout URL, enter https://localhost:5000/signout-oidc. 
       Also "ID Token" can be Unchecked under Authentication tab.
       
       To Register our Event Catalog API and Add a Scope, please follow sections "Register the Web API" and "Add a Scope" described in [this link](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-register-app#register-an-application).
       
       For our Front End to call Event Catalog API on behalf of the signed in user, they must request delegated permissions.
       For details please check out how to [Add permissions to access your Web API](https://learn.microsoft.com/en-us/azure/active-directory/develop/quickstart-configure-app-access-web-apis#add-permissions-to-access-your-web-api).       

     - Step 2: Code Configurations
       
       After app registration is done, we need to make code changes in our `appsettings.json` file for both projects with the ID values generated.

       Follow [this link](https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-web-app-call-api-app-configuration?view=aspnetcore-6.0&tabs=aspnetcore#client-secrets-or-client-certificates) to understand more how to make specific code changes in our `Startup.cs` and `appsettings.json`.

       To acquire a token, to access our Event Catalog service, changes are made in `EventCatalogController.cs` and `EventCatalogService.cs` of the **GloboTicket.Web project**.

       Please follow [acquire token link](https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-web-app-call-api-acquire-token?view=aspnetcore-6.0&tabs=aspnetcore) and [Call Web API link](https://learn.microsoft.com/en-us/azure/active-directory/develop/scenario-web-app-call-api-call-api?view=aspnetcore-6.0&tabs=aspnetcore#option-3-call-a-downstream-web-api-without-the-helper-class) to understand more on how to acquire tokens and call a Web API.

       To protect our Web API further, we can add Scopes in our Event Catalog Controller API. This protection ensures that the API is called only by Applications on behalf of users who have the right scopes and roles.
       
       Example:

       ```
       [HttpGet]
        [RequiredScope(new string[] { "Catalog.FullAccess" })]
        public async Task<ActionResult<IEnumerable<Models.EventDto>>> Get(
            [FromQuery] Guid categoryId)
        {
            ...ommited...
        }
       ```

   - Part 2, using [Dapr Bearer Middleware](https://docs.dapr.io/reference/components-reference/supported-middleware/middleware-bearer/) for Event Catalog API
     - Step 1: Register the sample application(s) in your tenant

       Same Steps to be followed as in Part 1, Step 1.

     - Step 2: Code Configurations

       For Frontend, the code changes remain the same , as explained in Part 1, Step 2.

       For Event Catalog API, following code changes to be made:
       
       - The `AzureAd` section can be removed from `appsettings.json` file and we can comment line `services.AddMicrosoftIdentityWebApiAuthentication(Configuration);` in  `Startup.cs` file (from  **_GloboTicket.Services.EventCatalog_** project)

       - Create a Dapr Bearer middleware component file under **_AzComponents_**, with Client ID to be filled from the App Registration done in Step 1 for Event Catalog project, and put appropriate 'Tenant-ID'

         ```
         apiVersion: dapr.io/v1alpha1
         kind: Component
         metadata:
           name: catalog-bearer-token
         spec:
           type: middleware.http.bearer
           version: v1
           metadata:
           - name: issuerURL
             value: "https://login.microsoftonline.com/<Tenant-ID>/v2.0"
           - name: clientID
             value: "<Client-ID>"
         ```

       - Create new config file say "catalogConfig.yml" under **_AzComponents_** folder like following:
         
         ```
         apiVersion: dapr.io/v1alpha1
         kind: Configuration
         metadata:
           name: catalogConfig
         spec:
           tracing:
           samplingRate: "1"
           zipkin:
             endpointAddress: http://localhost:9411/api/v2/spans
           metric:
             enabled: true
           httpPipeline:
             handlers:
             - name: catalog-bearer-token
               type: middleware.http.bearer
         ```
         *Note - You may need a new Zipkin endpoint, as it is already used in our main `config.yml`. You can still run the code as is, but you may see Zipkin warnings in VS Code debug tab only for Event Catalog service.

       - In our `tasks.json`, replace "config" path value for catalog app-id, like following:

         `"config": "./AzComponents/catalogConfig.yaml"`
          
   The bearer middleware helps you to make the Dapr API a protected resource where all clients should provide a bearer token in the Authorization header of the request. 
   Then before further processing the request, Dapr will check with the Identity Provider whether this bearer token is valid.

       
## Running the app locally   

   Once VS Code with [Dapr Extension](https://docs.dapr.io/developing-applications/ides/vscode/vscode-dapr-extension/) has been installed, we can leverage it to scaffold the configuration for us, instead of manually configuring **launch.json**.

   A **tasks.json** file also gets prepared by the Dapr extension task.

   Follow [this link](https://docs.dapr.io/developing-applications/ides/vscode/vscode-how-to-debug-multiple-dapr-apps/#prerequisites) to know more about configuring `launch.json and tasks.json`

   In VS Code go to Run and Debug, and Run All Projects at the same time or Individual project.

 ![debug](https://user-images.githubusercontent.com/84964657/190982955-b0a69850-4795-444a-aaf3-e2d6120dc1b2.jpg)
 
  All the projects which have build successfully and running, can be viewed in the Call Stack window.

![callstack](https://user-images.githubusercontent.com/84964657/190982330-5724fbae-2caa-49ec-a87a-db425db661c5.jpg)

   Once the application and side car is running, navigate to address **https://localhost:5000** in your preferred browser, to access the application.

   You're prompted for your credentials, and then asked to consent to the permissions that your app requires. Select Accept on the consent prompt.

   After consenting to the requested permissions, the app displays that you've successfully logged in using your Azure Active Directory credentials, and you'll see your email address in the "Api result" section of the page. This was extracted using Microsoft Graph.
   
   We can also apply breakpoint to debug the code. Check [this link](https://code.visualstudio.com/docs/editor/debugging#_breakpoints) for more info.

   ![breakpoint](https://user-images.githubusercontent.com/84964657/191080455-2aa1a8f9-a051-410b-9a42-617184b5ee39.jpg)

   The Darp extension added also provides information about the applications running and the corresponding components loaded for that application.

   ![dapr_extension_components](https://user-images.githubusercontent.com/84964657/190985678-5b7d24c8-095d-43e5-86fe-0002a5d985ee.png)   
   
## Troubleshooting notes

- If not able to load Dapr projects when running from VS Code, check if Docker Engine is running, so that it can load all components.
- Make sure the **Azure AD** and **EventCatalogScopes** placeholder values are filled in `appsettings.json` file for Front End Client and **Azure AD** placeholder values for Event Catalog project, after Azure AD App Registration is done.
  
  (If following Part 2, make the `appsettings.json` changes only for Front End project)
- When registering Front End Client in Azure AD, make sure Delegated Permissions are added for Event Catalog in API Permissions.
- If using Azure Service Bus as a Pub Sub Message broker make sure to enter primary connection string value for **"servicebus"** key in `secrets.json`
- If using Cosmos DB make sure to enter Endpoint and Key in `secrets.json` file **"CosmosDb"** section.
- If using Azure Redis Cache make sure to enter Key in `secrets.json` file **"redis"** section.
- If mail binding is not working, make sure `maildev`image is running. Refer [this link](https://github.com/maildev/maildev) for more info.
- For any more service issues, we can check Zipkin trace logs.   

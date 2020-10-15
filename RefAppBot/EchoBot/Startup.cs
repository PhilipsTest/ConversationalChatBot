// Copyright (c) Philips. All rights reserved.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.BotBuilderSamples.Bots;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Extensions.Hosting;

using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {

        //Cosmos Db Settings
        public static string TranscriptDatabaseId { get; set; }
        public static string TranscriptCollectionId { get; set; }
        public static string TranscriptAuthKey { get; set; }
        public static string CosmosDBEndpoint { get; set; }
        public static string ConversationStateCollectionName { get; set; }
        public static string UserStateCollectionName { get; set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private const string CosmosServiceEndpoint = "https://pz-ew-cosmos-np-digitaltrans-001.documents.azure.com:443/";
        private const string CosmosDBKey = "L2NPREph39uqCmawhpxJfRkTch1uigTihDH8VsNJ6ZQzq1p1oZztGfMB07ND4V31kz47d6eLIooaaXaKAgTHug==";
        private const string CosmosDBDatabaseName = "non-prod-cosmos-db";
        private const string CosmosDBCollectionName = "ems-chat-transcripts";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            TranscriptDatabaseId = CosmosDBDatabaseName;//Configuration.GetSection("CosmosDb:DatabaseName").Value;
            TranscriptCollectionId = CosmosDBCollectionName;// Configuration.GetSection("CosmosDb:TranscriptCollectionName").Value;
            TranscriptAuthKey = CosmosDBKey;// Configuration["AP-Prod-CosmosDBKey"];
            CosmosDBEndpoint = CosmosServiceEndpoint;// Configuration.GetSection("CosmosDb:Account").Value;
            ConversationStateCollectionName = Configuration.GetSection("CosmosDb:ConversationStateCollectionName").Value;
            UserStateCollectionName = Configuration.GetSection("CosmosDb:UserStateCollectionName").Value;

            services.AddControllers().AddNewtonsoftJson();

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            services.AddSingleton(new QnAMakerEndpoint
            {
                KnowledgeBaseId = "07dfcc61-3f61-4266-9ae2-0b02fe54dcf0",
                EndpointKey = "2d6250df-0986-48be-8f9b-fde8507b3c5b",
                Host = "https://qna-euw1-xlncare-uk-01-np.azurewebsites.net/qnamaker"
            });

            var cosmosDbStorageOptions = new CosmosDbPartitionedStorageOptions()
            {
                CosmosDbEndpoint = "https://pz-ew-cosmos-np-digitaltrans-001.documents.azure.com:443/",
                AuthKey = "L2NPREph39uqCmawhpxJfRkTch1uigTihDH8VsNJ6ZQzq1p1oZztGfMB07ND4V31kz47d6eLIooaaXaKAgTHug==",
                DatabaseId = "non-prod-cosmos-db",
                ContainerId = "ems-chat-transcripts"
            };
            var storage = new CosmosDbPartitionedStorage(cosmosDbStorageOptions);

            // Create the User state.  
            var userState = new UserState(storage);
            services.AddSingleton<UserState>(userState);

            // Create the Conversation state.  
            var conversationState = new ConversationState(storage);
            services.AddSingleton<ConversationState>(conversationState);

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, EchoBot>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}

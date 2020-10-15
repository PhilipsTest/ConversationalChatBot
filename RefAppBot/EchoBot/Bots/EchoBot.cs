// Copyright (c) Philips Corporation. All rights reserved.
//

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.AI.QnA;
using System.Linq;
using System;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class EchoBot : ActivityHandler
    {
        private BotState _conversationState;
        private BotState _userState;

        public QnAMaker EchoBotQnA { get; private set; }

        public EchoBot(QnAMakerEndpoint endpoint,ConversationState conversationState, UserState userState)
        {
            // connects to QnA Maker endpoint for each turn
            EchoBotQnA = new QnAMaker(endpoint);
            _conversationState = conversationState;
            _userState = userState;
        }


        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var results = await EchoBotQnA.GetAnswersAsync(turnContext);

            var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());

            var userStateAccessors = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile());
            var messageTimeOffset = (DateTimeOffset)turnContext.Activity.Timestamp;
            var localMessageTime = messageTimeOffset.ToLocalTime();
            conversationData.Timestamp = localMessageTime.ToString();
            conversationData.ChannelId = turnContext.Activity.ChannelId.ToString();
            conversationData.InConversationString = turnContext.Activity.Text;
            userProfile.conversations.Add(conversationData);
            userProfile.conversationID = turnContext.Activity.Id;

            if (results.Any())
            {
                var reply = MessageFactory.Text(results.First().Answer);
                var aQnAResult = results.First();
                var resultMetadata = aQnAResult.Metadata;
                if (aQnAResult.Context.Prompts.Length > 0)
                {
                    var suggestedActions = new SuggestedActions();
                    reply.SuggestedActions = suggestedActions;
                    reply.SuggestedActions.Actions = new List<CardAction>();
                    for (int i = 0; i < results.First().Context.Prompts.Length; i++)
                    {
                        string promptImageURL = null;
                        foreach (var promptMetaData in resultMetadata)
                        {
                            var optionNumber = i + 1;
                            if (promptMetaData.Name == optionNumber.ToString())
                            {
                                promptImageURL = "https://" + promptMetaData.Value;
                            }
                        }
                        //https://i.ibb.co/XSCmhvT/5b58db84b354cd1f008b45ce.jpg
                        var promptText = aQnAResult.Context.Prompts[i].DisplayText;
                        reply.SuggestedActions.Actions.Add(item: new CardAction() { Title = promptText, Type = ActionTypes.ImBack, Value = promptText, Image = promptImageURL });
                    }
                }
                conversationData.OutConversationString = reply.Text;
                // Respond with the result from the Qna Service
                await turnContext.SendActivityAsync(reply, cancellationToken);
            }
            else
            {
                // The QnA service did not have any results
                // Proceed with the previous code
                var aText = "Sorry was not able to understand the question. Please select below devices to continue.";
                var aCard = MessageFactory.Text(aText);
                var suggestedActions = new SuggestedActions();
                aCard.SuggestedActions = suggestedActions;
                aCard.SuggestedActions.Actions = new List<CardAction>();
                var suggestion1 = "Tuscany";
                var suggestion2 = "Osla";
                aCard.SuggestedActions.Actions.Add(item: new CardAction() { Title = suggestion1, Type = ActionTypes.ImBack, Value = suggestion1 });
                aCard.SuggestedActions.Actions.Add(item: new CardAction() { Title = suggestion2, Type = ActionTypes.ImBack, Value = suggestion2 });
                await turnContext.SendActivityAsync(aCard, cancellationToken);
            }
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {

            var activity = turnContext.Activity;

            if (string.IsNullOrWhiteSpace(activity.Text) && activity.Value != null)
            {
                activity.Text = JsonConvert.SerializeObject(activity.Value);
            }
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.  
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    //Welcome Card
                    var welcomeText = "Hi! I am your Philips Sonicare Brush advice bot."; //"Hi, I am your Philips assistant and welcome!. Please select the product to assist you." + "\n\n" + "Philips Privacy Policy : " + "https://www.philips.com/a-w/privacy-notice.html";
                    var welcomeCard = MessageFactory.Text(welcomeText);
                    await turnContext.SendActivityAsync(welcomeCard, cancellationToken);

                    //Welcome card two
                    var welcomeText1 = "I can help you to find out the best product and I can also resolve the connectivity issues you are facing with your device"; //"Hi, I am your Philips assistant and welcome!. Please select the product to assist you." + "\n\n" + "Philips Privacy Policy : " + "https://www.philips.com/a-w/privacy-notice.html";
                    var welcomeCard1 = MessageFactory.Text(welcomeText1);
                    _ = await turnContext.SendActivitiesAsync(
                             new Activity[] {
                                    new Activity { Type = "delay", Value= 1000 },
                                    welcomeCard1,
                            },
                    cancellationToken);
                   
                   

                    //Privacy card..
                    var privacyText = "**Philips Privacy Notice.** A transcript of this chat is retained with us for quality and training purposes.Chat data collected may be linked to case reference id in cases where the assistant is unable to resolve the query and support is required from Philips Consumer Care.Philips values and respects your privacy.Please read our [privacy notice](https://www.philips.co.in/content/corporate/en_IN/privacy-notice.html/) for more information.";
                    var privacyCard = MessageFactory.Text(privacyText);
                    _ = await turnContext.SendActivitiesAsync(
                             new Activity[] {
                                                 new Activity { Type = "delay", Value= 1500 },
                                                 privacyCard,
                            },
                    cancellationToken);

                    //Welcome card with user options
                    var welcomeText2 = "Please let us know what type of product you own";
                    var welcomeCard2 = MessageFactory.Text(welcomeText2);
                    var suggestedActions = new SuggestedActions();
                    welcomeCard2.SuggestedActions = suggestedActions;
                    welcomeCard2.SuggestedActions.Actions = new List<CardAction>();
                    var suggestion1 = "SoniCare SmartClean";
                    var suggestion1CTN = "SoniCare SmartClean HX9903/11";
                    var suggertion1Image = "https://www.usa.philips.com/c-dam/b2c/master/experience/consistency-campaign/diamondclean-smart/philips-sonicare-diamondclean_smart-with-glass_charger-black-HX9903_11.png";
                    var suggestion2 = "SoniCare Clean";
                    var suggestion2CTN = "SoniCare Clean HX6853/11";
                    var suggestion2Image = "https://images.philips.com/is/image/PhilipsConsumer/HX6853_11-IMS-en_US?wid=840&hei=720&$jpglarge$";
                    welcomeCard2.SuggestedActions.Actions.Add(item: new CardAction() { Title = suggestion1, Type = ActionTypes.ImBack, Value = suggestion1CTN, Image = suggertion1Image });
                    welcomeCard2.SuggestedActions.Actions.Add(item: new CardAction() { Title = suggestion2, Type = ActionTypes.ImBack, Value = suggestion2CTN, Image = suggestion2Image });

                    _ = await turnContext.SendActivitiesAsync(
                             new Activity[] {
                             new Activity { Type = "delay", Value= 3500 },
                             welcomeCard2,
                            },
                    cancellationToken);
                    return;
                }
            }
        }
    }



    public class UserProfile
    {
        public string conversationID { get; set; }
        public List<ConversationData> conversations { get; } = new List<ConversationData>();
    }

    public class ConversationData
    {
        // The time-stamp of the most recent incoming message.  
        public string Timestamp { get; set; }

        // The ID of the user's channel.  
        public string ChannelId { get; set; }

        //User selected option ConversationString
        public string InConversationString { get; set; }

        //Out ConversationString
        public string OutConversationString { get; set; }

    }

}




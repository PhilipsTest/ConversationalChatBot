// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.AI.QnA;
using System.Linq;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class EchoBot : ActivityHandler
    {
        public QnAMaker EchoBotQnA { get; private set; }
        public EchoBot(QnAMakerEndpoint endpoint)
        {
            // connects to QnA Maker endpoint for each turn
            EchoBotQnA = new QnAMaker(endpoint);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var results = await EchoBotQnA.GetAnswersAsync(turnContext);
            if (results.Any())
            {
                var reply = MessageFactory.Text(results.First().Answer);
                if (results.First().Context.Prompts.Length > 0)
                {
                    var suggestedActions = new SuggestedActions();
                    reply.SuggestedActions = suggestedActions;
                    reply.SuggestedActions.Actions = new List<CardAction>();
                    for (int i = 0; i < results.First().Context.Prompts.Length; i++)
                    {
                        var promptText = results.First().Context.Prompts[i].DisplayText;
                        reply.SuggestedActions.Actions.Add(item: new CardAction() { Title = promptText, Type = ActionTypes.ImBack, Value = promptText });
                    }
                }

                // Respond with the result from the Qna Service
                await turnContext.SendActivityAsync(reply, cancellationToken);
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var welcomeText = "Hi! I am the SleepMapper assistant.I can help you with the connectivity issues you are facing with your device"; //"Hi, I am your Philips assistant and welcome!. Please select the product to assist you." + "\n\n" + "Philips Privacy Policy : " + "https://www.philips.com/a-w/privacy-notice.html";
                    var welcomeCard = MessageFactory.Text(welcomeText);
                    await turnContext.SendActivityAsync(welcomeCard, cancellationToken);
                    //Privacy card..
                    var privacyText = "**Philips Privacy Notice** \n\n A transcript of this chat is retained with us for quality and training purposes.Chat data collected may be linked to case reference id in cases where the assistant is unable to resolve the query and support is required from Philips Consumer Care.Philips values and respects your privacy.Please read our [privacy notice](https://www.philips.co.in/content/corporate/en_IN/privacy-notice.html/) for more information.";
                    var privacyCard = MessageFactory.Text(privacyText);
                    var suggestedActions = new SuggestedActions();
                    privacyCard.SuggestedActions = suggestedActions;
                    privacyCard.SuggestedActions.Actions = new List<CardAction>();
                    var suggestion1 = "Tuscany";
                    var suggestion2 = "Osla";
                    privacyCard.SuggestedActions.Actions.Add(item: new CardAction() { Title = suggestion1, Type = ActionTypes.ImBack, Value = suggestion1 });
                    privacyCard.SuggestedActions.Actions.Add(item: new CardAction() { Title = suggestion2, Type = ActionTypes.ImBack, Value = suggestion2 });

                    _ = await turnContext.SendActivitiesAsync(
                             new Activity[] {
                             new Activity { Type = "delay", Value= 1000 },
                             privacyCard,
                            },
                    cancellationToken);
                    return;
                }
            }
        }
    }
}

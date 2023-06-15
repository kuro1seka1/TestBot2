using Microsoft.VisualBasic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using TestBot2;
using File = System.IO.File;

var botClient = new TelegramBotClient("6253942110:AAHrDIXUsN3LXxFLDncrFcbgrETqMnY1lV8");

  Quiz quiz;

Dictionary<long, QuestionState> States;
Dictionary<long, int> PlayerScore;
string pathToLog = "log.txt";

ExcelLayout excel = new();
excel.Excel(@"C:\Users\Василий\Desktop\Kursk.xls");

quiz = new Quiz("testKursk.txt");

States = new Dictionary<long, QuestionState>();

PlayerScore = new Dictionary<long, int>();


//Bot.on += BotOnCallbackQueryReceived;
using CancellationTokenSource cts = new();
var me = botClient.GetMeAsync().Result;
CancellationTokenSource cancelTokenSource = new CancellationTokenSource();

Console.WriteLine(me.FirstName);


// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);



Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    
    if (update.Message is not { } message)
        return;
    
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

    
    
    var offset = 0;
    try
    {
        
            var updates = await botClient.GetUpdatesAsync(offset);

            Console.WriteLine($"New message received from {message.Chat.FirstName}: {message.Text} in {chatId}");

        if (message.Text == "/start")
        {

                        string text =
            @"Здравствуйте, на ответ дается 6 секунд.
            Если ответ не будет дан за это время, все зависимости от его правильности,
            он будет считаться не правильным!
            Итоговые результаты вы сможете увидеть в конце опроса.
            Набор команд:
            /quiz - Начать опрос";
                        await botClient.SendTextMessageAsync(chatId, text);
        }

        
        else if(message.Text == "/quiz")
        {
            excel.Excel(@"C:\Users\Василий\Desktop\Kursk.xls");
            offset = update.Id + 1;
            var playerId = message.From.Id;
            if (!States.TryGetValue(chatId, out var state))
            {
                state = new QuestionState();
                States[chatId] = state;
            }

            if (state.CurrentItem == null)
            {
                state.CurrentItem = quiz.NextQuestion();
                state.questionIndex = 0;
            }
            Stopwatch sw = new();

            while (state.questionIndex < quiz.Questions.Count)
            {
                var question = state.CurrentItem;
                await botClient.SendTextMessageAsync(chatId, question.Question);

                var answer = "";
                while (string.IsNullOrEmpty(answer))
                {
                    updates = await botClient.GetUpdatesAsync(offset: offset, timeout: 100);
                    message = updates.FirstOrDefault()?.Message;

                    if (message != null && !string.IsNullOrEmpty(message.Text))
                    {
                        answer = message.Text.ToLower();
                    }
                }
                sw.Start();

                offset = updates.LastOrDefault().Id + 1;

                if (answer == question.Answer.ToLower() && sw.ElapsedMilliseconds < 6000)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Верно!",
                        cancellationToken: cancellationToken); ;
                    if (PlayerScore.ContainsKey(playerId))
                    {
                        PlayerScore[playerId]++;
                    }
                    else
                    {
                        PlayerScore[playerId] = 1;
                    }

                }
                else
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Не верно !",
                        cancellationToken: cancellationToken);
                }

                state.CurrentItem = quiz.NextQuestion();
                state.questionIndex++;
                sw.Stop();

                Console.WriteLine(sw.ElapsedMilliseconds);
                sw.Restart();

            }

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Тест окончен!",
                cancellationToken: cancellationToken);
            int score = PlayerScore[playerId];
            int test = quiz.Questions.Count;
            using (StreamWriter swr = new StreamWriter(pathToLog, true))
            {
                await swr.WriteLineAsync(($"{message.Chat.FirstName} набрал {score} очков в {DateTime.Now}"));
            }

            if (score == 0)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Ни одного правильного ответа",
                    cancellationToken: cancellationToken);

                PlayerScore[playerId] = 0;
                state.questionIndex = 0;
                
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: chatId,
                    text: $"Вы набрали {score} из {test} очков ",
                    cancellationToken: cancellationToken);


                PlayerScore[playerId] = 0;
                state.questionIndex = 0;


                

            }

          
        }

                    


                
           if (update.CallbackQuery != null)
{
                var callbackQuery = update.CallbackQuery;
                string buttonText = callbackQuery.Data;

                Console.WriteLine($"Callback query received from {callbackQuery.From.FirstName}: {buttonText}");

                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: $"You pressed {buttonText}");
}
offset = update.Id + 1;

await Task.Delay(1000);
File.Delete("testKursk.txt");
cancelTokenSource.Cancel();
        

        

                           



                // Echo received message text

            

            Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                var ErrorMessage = exception switch
                {
                    ApiRequestException apiRequestException
                        => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                Console.WriteLine(ErrorMessage);
                return Task.CompletedTask;
            }
        


    }
    catch
    {

    }
}
       







Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}
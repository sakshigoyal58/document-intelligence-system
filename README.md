┌──────────────────────────────────────────────┐
│                FRONTEND                      │
├──────────────────────────────────────────────┤
│ Next.js + React + Tailwind                  │
│ - Login UI                                  │
│ - Upload PDF                                │
│ - Ask Questions                             │
└─────────────────────┬────────────────────────┘
                      │ HTTPS + JWT
                      ▼

┌──────────────────────────────────────────────┐
│              API GATEWAY                     │
├──────────────────────────────────────────────┤
│ AWS API Gateway (HTTP API)                  │
│ - Route requests                            │
│ - JWT validation                            │
└──────────────┬───────────────────────────────┘
               │
       ┌───────┴──────────────────────────────────────┐
       ▼                                              ▼

┌──────────────────────────┐         ┌──────────────────────────┐
│ UploadLambda             │         │ QueryLambda              │
├──────────────────────────┤         ├──────────────────────────┤
│ AWS Lambda (.NET 8)      │         │ AWS Lambda (.NET 8)      │
│                          │         │                          │
│ Responsibilities:        │         │ Responsibilities:        │
│ - receive PDF upload     │         │ - receive user question  │
│ - store file in S3       │         │ - fetch chunks           │
│ - save metadata          │         │ - call AIService         │
│ - send SQS message       │         │ - return AI answer       │
└──────────────┬───────────┘         └──────────────┬───────────┘
               │                                    │
               ▼                                    ▼

     ┌──────────────────────┐         ┌──────────────────────┐
     │ Amazon S3            │         │ DynamoDB             │
     ├──────────────────────┤         ├──────────────────────┤
     │ Store PDF documents  │         │ Store:               │
     │                      │         │ - metadata           │
     │                      │         │ - chunks             │
     │                      │         │ - summaries          │
     └──────────┬───────────┘         └──────────┬───────────┘
                │                                ▲
                ▼                                │

     ┌───────────────────────────────────────────┐
     │              Amazon SQS                   │
     ├───────────────────────────────────────────┤
     │ Main Queue                               │
     │ - document processing event              │
     │                                          │
     │ DLQ (Dead Letter Queue)                  │
     │ - failed events after retries            │
     └──────────────────┬────────────────────────┘
                        ▼

┌──────────────────────────────────────────────┐
│ ProcessingLambda                             │
├──────────────────────────────────────────────┤
│ AWS Lambda (.NET 8)                          │
│                                              │
│ Responsibilities:                            │
│ - read PDF from S3                           │
│ - extract text                               │
│ - chunk text                                 │
│ - call AIService                             │
│ - generate summary                           │
│ - save chunks/results in DynamoDB            │
└──────────────────┬───────────────────────────┘
                   ▼

┌──────────────────────────────────────────────┐
│ AIService (.NET Class Library)               │
├──────────────────────────────────────────────┤
│ Claude API OR OpenAI API                     │
│                                              │
│ Features:                                    │
│ - summarization                              │
│ - Q&A                                        │
│ - extraction                                 │
│ - classification                             │
│                                              │
│ Prompt Engineering Used                      │
└──────────────────────────────────────────────┘


┌──────────────────────────────────────────────┐
│ NotificationLambda                           │
├──────────────────────────────────────────────┤
│ AWS Lambda (.NET 8)                          │
│                                              │
│ Responsibilities:                            │
│ - consume notification events                │
│ - retry failed notifications                 │
│ - handle DLQ events                          │
└──────────────────────────────────────────────┘


┌──────────────────────────────────────────────┐
│ Shared .NET Class Libraries                  │
├──────────────────────────────────────────────┤
│ Core                                         │
│ - models                                     │
│ - DTOs                                       │
│                                              │
│ Services                                     │
│ - AIService                                  │
│ - S3Service                                  │
│ - DynamoDbService                            │
│ - JwtService                                 │
│ - SqsService                                 │
│                                              │
│ Business                                     │
│ - chunking logic                             │
│ - validation                                 │
│ - RBAC rules                                 │
│                                              │
│ Infrastructure                               │
│ - repositories                               │
│ - AWS SDK integrations                       │
│ - logging                                    │
└──────────────────────────────────────────────┘


┌──────────────────────────────────────────────┐
│ CloudWatch                                   │
├──────────────────────────────────────────────┤
│ - Lambda logs                                │
│ - SQS monitoring                             │
│ - DLQ alarms                                 │
│ - API monitoring                             │
└──────────────────────────────────────────────┘

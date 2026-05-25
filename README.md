# 📄 AI Document Intelligence System (Serverless on AWS)

A fully serverless, AI-powered document processing and Q&A system built using **.NET 8, AWS Lambda, and Claude/OpenAI APIs**.  
The system allows users to upload PDF documents, automatically processes them, and enables AI-powered querying over document content.

---

# 🚀 Architecture Overview

## 🧑‍💻 Frontend
- Next.js + React + Tailwind
- Features:
  - User Authentication (JWT)
  - Upload PDF documents
  - Ask questions on documents

---

## 🌐 API Layer
### AWS API Gateway (HTTP API)
- Routes all incoming requests
- Handles JWT authentication
- Forwards requests to Lambda functions

---

## ⚙️ Backend (AWS Lambda - .NET 8)

### 📤 UploadLambda
- Receives PDF upload requests
- Stores files in Amazon S3
- Saves metadata in DynamoDB
- Publishes event to SQS

---

### ❓ QueryLambda
- Handles user questions
- Fetches document chunks from DynamoDB
- Calls AI service for response generation
- Returns AI-generated answer

---

### 📄 ProcessingLambda
- Triggered via SQS
- Reads PDF from S3
- Extracts raw text
- Splits text into chunks
- Stores processed data in DynamoDB
- Calls AI service for summarization

---

### 🔔 NotificationLambda
- Consumes notification events from SQS
- Handles retry logic
- Processes DLQ (Dead Letter Queue) messages

---

## ☁️ AWS Services

### 🗂️ Amazon S3
- Stores uploaded PDF documents

### 🗃️ Amazon DynamoDB
- Stores:
  - Document metadata
  - Text chunks
  - AI-generated summaries

### 📬 Amazon SQS
- Handles asynchronous processing
- Enables event-driven architecture
- Includes DLQ for failed message handling

---

## 🤖 AI Layer

### AIService (.NET Class Library)
- Integrates with:
  - Claude API OR OpenAI API

### Capabilities:
- Document summarization
- Question answering (Q&A)
- Information extraction
- Text classification

### Prompt Engineering:
- Context-aware prompts
- Chunk-based processing
- Reduced hallucination design

---

## 🧱 Shared .NET Architecture

### Core
- Models
- DTOs
- Enums

### Services
- AIService
- S3Service
- DynamoDbService
- JwtService
- SqsService

### Business
- Chunking logic
- Validation rules
- RBAC policies

### Infrastructure
- AWS SDK integrations
- Repository implementations
- Logging system

---

## 🔐 Security

- JWT-based authentication
- Role-Based Access Control (RBAC)
  - Admin: Full access
  - User: Own documents only

---

## 📊 Observability

### Amazon CloudWatch
- Lambda logs
- API monitoring
- SQS tracking
- DLQ alerts

---

## 🔄 End-to-End Flow

1. User uploads PDF via frontend
2. API Gateway routes request to UploadLambda
3. PDF stored in S3
4. Metadata saved in DynamoDB
5. SQS event triggers ProcessingLambda
6. PDF is parsed and chunked
7. AI generates summaries
8. User queries document via QueryLambda
9. AI returns context-aware answers

---

## 🧠 Key Design Principles

- Serverless-first architecture
- Event-driven communication (SQS)
- Clean Architecture (.NET)
- Separation of concerns
- Scalable AI integration layer
- Cost-efficient AWS design (Free Tier optimized)

---

## 🛠️ Tech Stack

- .NET 8 (AWS Lambda)
- AWS API Gateway
- AWS Lambda
- Amazon S3
- Amazon DynamoDB
- Amazon SQS + DLQ
- CloudWatch
- Next.js + React + Tailwind
- Claude API / OpenAI API

---

## 📌 Project Goal

To demonstrate:
- Real-world serverless architecture design
- AI integration in backend systems
- Event-driven distributed systems
- Clean .NET architecture in cloud environments

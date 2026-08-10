# Security

## Authentication

Amazon Cognito provides user authentication and identity.

JWT-based authentication is used to protect backend operations.

The backend should independently validate authentication information rather than relying on the frontend to determine whether a request is trusted.

---

## Authorization

Authorization must be enforced by backend services.

Frontend role checks are only a UX mechanism and must not be treated as the security boundary.

For document operations, the backend should validate:

* The authenticated user's identity.
* The user's application role.
* Whether the user is authorized to access the requested document.

This prevents a user from bypassing UI restrictions by directly calling backend APIs.

---

## S3 Upload Security

The browser uses short-lived presigned URLs to upload documents directly to S3.

This approach avoids exposing AWS credentials to the browser and prevents large document payloads from passing through the application API.

Recommended controls include:

* Short presigned URL expiry.
* Restricted S3 key prefixes.
* Maximum object size.
* Allowed content types.
* Server-side file validation.
* Post-upload processing validation.
* Document ownership checks.

---

## Secrets

The following must never be committed to source control:

* Gemini/API keys.
* AWS access keys.
* Cognito secrets.
* Database credentials.
* Private certificates.
* Other service credentials.

Use environment-specific secret management for deployed workloads.

For local development, use environment variables or an appropriate local secret-management mechanism.

---

## IAM

Each Lambda should have a dedicated execution role containing only the permissions required by that workload.

For example:

```text
Processing Lambda
    ↓
S3 read
SQS write
DynamoDB update
```

should not automatically receive:

```text
AdministratorAccess
```

Least-privilege IAM reduces the impact of compromised application components.

---

## OpenSearch

OpenSearch should not be publicly accessible unless there is a specific architectural requirement.

Where possible:

* Keep the domain in private networking.
* Restrict network access.
* Use IAM/resource policies.
* Encrypt traffic.
* Enable encryption at rest.
* Restrict access to trusted workloads.

---

## Data Protection

Sensitive documents and metadata should be protected both in transit and at rest.

Recommended controls include:

* HTTPS/TLS for network communication.
* S3 encryption.
* DynamoDB encryption.
* SQS encryption where applicable.
* OpenSearch encryption.
* Controlled access to logs and monitoring systems.

---

## Document Security

Documents can originate from users and should therefore be treated as untrusted input.

Production deployments should consider:

* File-type validation.
* File-size limits.
* Content validation.
* Malware scanning.
* Safe document parsing.
* Controlled S3 object prefixes.
* Document ownership validation.

---

## Production Hardening

Additional security controls recommended for production include:

* Centralized audit logging.
* Security monitoring and alerting.
* Secret/key rotation.
* API rate limiting.
* WAF where appropriate.
* Dependency vulnerability scanning.
* Infrastructure security scanning.
* Authentication failure monitoring.
* Suspicious-access detection.
* Automated security testing.

The security architecture should evolve with the application's deployment environment and threat model.

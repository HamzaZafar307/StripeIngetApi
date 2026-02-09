# Stripe Ingestion Service

## Setup

### Prerequisites
-   [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Quick Start
1.  **Run Application**:
    ```bash
    docker-compose up --build
    ```

2.  **Access API**:
    -   Swagger UI: [http://localhost:5185/swagger](http://localhost:5185/swagger)
    -   Webhook Endpoint: [http://localhost:5185/api/webhook](http://localhost:5185/api/webhook)

## Endpoints

-   `POST /api/webhook`: Ingest Stripe events.

## Lifecycle Testing

The service supports the full subscription lifecycle. Use **Swagger UI** (`http://localhost:5185/swagger`) to verify:

1.  **New Subscription**: Creates history with "new" change type.
2.  **Renewal**: Logs "renewal" event (no MRR change) via `customer.subscription.updated`.
3.  **Upgrade**: Detects price increase (Expansion MRR).
4.  **Downgrade**: Detects price decrease (Contraction MRR).
5.  **Cancellation**: Sets MRR to 0 (Churn MRR).
 
For detailed step-by-step instructions and JSON payloads, refer to **[walkthrough.md](walkthrough.md)**.

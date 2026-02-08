# Stripe Ingestion Service

## Setup

### Prerequisites
-   [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Quick Start
1.  **Run Application**:
    ```bash
    docker-compose up --build
    ```
    *This will start SQL Server and the API, automatically applying database migrations.*

2.  **Access API**:
    -   Swagger UI: [http://localhost:5185/swagger](http://localhost:5185/swagger)
    -   Webhook Endpoint: [http://localhost:5185/api/webhook](http://localhost:5185/api/webhook)

## Endpoints

-   `POST /api/webhook`: Ingest Stripe events.

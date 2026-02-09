# Walkthrough - Stripe Ingestion Service

## Overview
This document outlines the implementation of the Stripe Ingestion Service, designed to process webhook events, maintain subscription state, and produce MRR reports.

## Implementation Details

### Core Components
- **StripeIngest.Api**: .NET 9 Web API project.
- **Database**: SQL Server (Dockerized).
- **ORM**: Entity Framework Core 9.0.

### Key Logic
- **Event Processing**: Idempotent handling of `customer.subscription.*` events.
- **State Management**: Updates `CurrentSubscriptions` and records `SubscriptionHistory`.
- **MRR Calculation**: Computes MRR based on `quantity * unit_amount`.


## Verification

### Automated Tests
Integration tests cover:
- **New Subscriptions**: Correctly creates state and history with "new" change type.
- **Upgrades**: Detects MRR increase and logs "upgrade".
- **Enhanced Documentation**: Swagger UI now includes XML comments for endpoints and **sample Monthly/Yearly JSON payloads** in the Webhook description.
- **Reporting**: SQL Views for efficient MRR calculation., sets MRR to 0, and logs "churn".
- **Idempotency**: Duplicate events are ignored.

Run tests using:
```bash
dotnet test StripeIngest.Tests
```
**Status**: Passed.


{
    "month": "2024-05",
    "newMRR": 10000.00,
    "expansionMRR": 10000.00,
    "contractionMRR": 0,
    "churnedMRR": -20000.00,
    "netMRRChange": 0.00
}
```

## Step-by-Step Testing Guide

Use **Swagger UI** (`http://localhost:5186/swagger`) to send these payloads to **`POST /api/Webhook`**.

### 1. New Subscription (New)
Creates a subscription with $100 MRR.

**Payload:**
```json
{
  "id": "evt_lifecycle_1",
  "type": "customer.subscription.created",
  "created": 1704067200,
  "data": {
    "object": {
      "id": "sub_test_1",
      "customer": "cus_test_1",
      "status": "active",
      "items": { "data": [ { "quantity": 1, "plan": { "id": "price_100", "product": "prod_1", "amount": 10000, "currency": "usd", "interval": "month" } } ] }
    }
  }
}
```
{
  "id": "evt_lifecycle_2",
  "type": "invoice.paid",
  "created": 1706745600,
  "data": {
    "object": {
      "subscription": "sub_test_1",
      "customer": "cus_test_1",
      "amount_paid": 10000,
      "status": "paid"
    }
  }
}
```

### 2. Upgrade (Expansion)
Increases amount to $200. Logs "upgrade" and +$100 MRR Expansion.

**Payload:**
```json
{
  "id": "evt_lifecycle_3",
  "type": "customer.subscription.updated",
  "created": 1709251200,
  "data": {
    "object": {
      "id": "sub_test_1",
      "customer": "cus_test_1",
      "status": "active",
      "items": { "data": [ { "quantity": 1, "plan": { "id": "price_200", "product": "prod_2", "amount": 20000, "currency": "usd", "interval": "month" } } ] }
    }
  }
}
```

### 3. Downgrade (Contraction)
Decreases amount back to $100. Logs "downgrade" and -$100 MRR Contraction.

**Payload:**
```json
{
  "id": "evt_lifecycle_4",
  "type": "customer.subscription.updated",
  "created": 1711929600,
  "data": {
    "object": {
      "id": "sub_test_1",
      "customer": "cus_test_1",
      "status": "active",
      "items": { "data": [ { "quantity": 1, "plan": { "id": "price_100", "product": "prod_1", "amount": 10000, "currency": "usd", "interval": "month" } } ] }
    }
  }
}
```

### 4. Cancellation (Churn)
Cancels the subscription. Logs "churn" and -$100 MRR (MRR becomes 0).

**Payload:**
```json
{
  "id": "evt_lifecycle_5",
  "type": "customer.subscription.deleted",
  "created": 1714521600,
  "data": {
    "object": {
      "id": "sub_test_1",
      "customer": "cus_test_1",
      "status": "canceled",
      "items": { "data": [ { "quantity": 1, "plan": { "id": "price_100", "product": "prod_1", "amount": 10000, "currency": "usd", "interval": "month" } } ] }
    }
  }
}
```

### Verification
After sending these, check the history:
**`GET /api/Reports/customer/cus_test_1/history`**

---

## Setup & Run

### Quick Start (Docker)
1. Run application: `docker-compose up --build` (Auto-migrates DB)
2. Access Swagger: `http://localhost:5185/swagger`
3. Webhook Endpoint: `http://localhost:5185/api/webhook`

5. Get All Events: `GET http://localhost:5185/api/events`
6. Get Yearly MRR: `GET http://localhost:5185/api/reports/mrr/yearly`
7. Get Customer History: `GET http://localhost:5185/api/reports/customer/{customerId}/history`

### Multi-Item Subscriptions
The system supports subscriptions with multiple items (e.g., creating a subscription with 2 products). The MRR is calculated as the sum of all items (`quantity * amount`).

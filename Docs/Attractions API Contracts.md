# Attractions API Contracts
---
This RESTful API is only for objects stored in the Locations Database. User and site-specific data can be stored on the Web Server Database.
# Global Error Model
Define a **standard error response** used across all endpoints:

```json
{  
	"error": {  
	    "code": "string (machine-readable)",  
	    "message": "string (human-readable)",  
	    "details": "optional additional info",  
	    "requestId": "string (for tracing)"  
	}
}
```
# Common HTTP Error Codes

These apply to _all endpoints_:

| Code | Meaning                                            |
| ---- | -------------------------------------------------- |
| 400  | Bad Request (invalid params, malformed UUID, etc.) |
| 401  | Unauthorized (missing/invalid auth)                |
| 403  | Forbidden (no permission)                          |
| 404  | Resource not found                                 |
| 409  | Conflict (duplicate, constraint violation)         |
| 422  | Validation error                                   |
| 429  | Rate limited                                       |
| 500  | Internal server error                              |

---
# Park Endpoint
## GET `/Parks/{id}`
### Success
```json
{
	"id": "string (UUID)",
	"name": "string",
	"resort": "string",
	"city": "string",
	"country": "string",
	"latitude": "latitude",
	"longitude": "longitude",
	"createdAt": "timestamp",
	"updatedAt": "timestamp"
}
```
### Errors
```
400 Bad Request  
  - Invalid UUID format  
404 Not Found  
  - Park does not exist  
429 Too Many Requests  
500 Internal Server Error
```
# Attraction Endpoint
## GET `/Attractions/{id}`
### Success
```json
{
	"id": "string (UUID)",
	"parkId": "string (UUID)",
	"images": ["string"],
	"name": "string",
	"body": "string",
	"category": "string",
	"tags": ["string"],
	"latitude": "latitude",
	"longitude": "longitude",
	"createdAt": "timestamp",
	"updatedAt": "timestamp"
}
```
### Errors
```
400 Bad Request  
  - Invalid UUID format  
404 Not Found  
  - Attraction not found  
409 Conflict  
  - Referenced parkId does not exist (if validating relational integrity)  
422 Unprocessable Entity  
  - Invalid category or malformed fields
429 Too Many Requests
500 Internal Server Error
```
# Image Endpoint
## GET `/Attractions/image/{id}`
### Success
```json
(jpeg binary image data)
```
### Errors
```
400 Bad Request  
  - Invalid file name  
404 Not Found  
  - Image does not exist  
415 Unsupported Media Type  
  - Requested format not supported  
429 Too Many Requests  
500 Internal Server Error
```
# List Park IDs
## GET `/Parks`
### Query Parameters
```
limit: integer (default 50, max 200)  
offset: integer (default 0)  
country: string (optional filter)  
resort: string (optional filter)  
updatedAfter: timestamp (optional)
```
### Response (200 OK)
```json
{  
	"total": int,  
	"limit": int,  
	"offset": int,  
	"ids": ["string (uuid)"]
}
```

### Errors
```
400 Bad Request
  - Invalid query parameter (e.g., negative limit)
422 Unprocessable Entity
  - Invalid timestamp format
429 Too Many Requests
500 Internal Server Error
```
# List Attraction IDs
## GET `/Attractions`
### Query Parameters
```
limit: integer (default 50, max 200)  
offset: integer (default 0)  
parkId: UUID (optional filter)  
category: string (optional)  
tag: string (optional)  
updatedAfter: timestamp (optional)
```
### Response (200 OK)
```json
{  
	"total": int,  
	"limit": int,  
	"offset": int,  
	"ids": ["string (uuid)"]
}
```
### Errors
```
400 Bad Request
  - Invalid UUID format (parkId)
404 Not Found
  - parkId does not exist (optional depending on design)
422 Unprocessable Entity
  - Invalid category/tag
429 Too Many Requests
500 Internal Server Error
```
This RESTful API is only for objects stored in the Locations Database. User and site-specific data can be stored on the Web Server Database.
## Park Object
`/park/{id}`
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
## Attraction Object
`/attraction/{id}`
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
## Image Object
`attraction/image/{name}`
```json
(image data)
```
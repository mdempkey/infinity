**ER Diagram:**
```mermaid
erDiagram
    USER ||--o{ REVIEW : leaves
    USER ||--o{ RATING : leaves
```
## Review
- ID - PK
- User ID
- Attraction ID
- Content - Text
## Rating
- ID - PK
- User ID
- Attraction ID
- Rating - 0-5
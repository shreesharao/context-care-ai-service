# context-care-ai-service
Provides AI assisted suggestion and conversation

## Dependencies
### pgvector
docker run --rm -d --name postgres -p5432:5432 -e POSTGRES_DB=SnapshotDB -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=db@123 pgvector/pgvector:pg17
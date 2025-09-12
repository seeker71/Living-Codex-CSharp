-- Living Codex Database Initialization Script
-- This script sets up the initial database schema for the Living Codex system

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Create schemas
CREATE SCHEMA IF NOT EXISTS codex;
CREATE SCHEMA IF NOT EXISTS security;
CREATE SCHEMA IF NOT EXISTS monitoring;

-- Create users table
CREATE TABLE IF NOT EXISTS security.users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'user',
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create sessions table
CREATE TABLE IF NOT EXISTS security.sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES security.users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL,
    refresh_token VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    is_active BOOLEAN NOT NULL DEFAULT true
);

-- Create audit logs table
CREATE TABLE IF NOT EXISTS security.audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    action VARCHAR(100) NOT NULL,
    description TEXT,
    user_id UUID REFERENCES security.users(id),
    ip_address INET,
    user_agent TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create nodes table
CREATE TABLE IF NOT EXISTS codex.nodes (
    id VARCHAR(255) PRIMARY KEY,
    type_id VARCHAR(255) NOT NULL,
    state VARCHAR(50) NOT NULL,
    locale VARCHAR(10),
    title TEXT,
    description TEXT,
    content JSONB,
    meta JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create concepts table
CREATE TABLE IF NOT EXISTS codex.concepts (
    id VARCHAR(255) PRIMARY KEY,
    service_id VARCHAR(255) NOT NULL,
    concept_data JSONB NOT NULL,
    version VARCHAR(50) NOT NULL DEFAULT '1.0.0',
    status VARCHAR(50) NOT NULL DEFAULT 'active',
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create translations table
CREATE TABLE IF NOT EXISTS codex.translations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    concept_id VARCHAR(255) NOT NULL,
    source_language VARCHAR(10) NOT NULL,
    target_language VARCHAR(10) NOT NULL,
    translated_content JSONB NOT NULL,
    translation_metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create contributions table
CREATE TABLE IF NOT EXISTS codex.contributions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES security.users(id),
    concept_id VARCHAR(255) NOT NULL,
    contribution_type VARCHAR(50) NOT NULL,
    content JSONB NOT NULL,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create performance metrics table
CREATE TABLE IF NOT EXISTS monitoring.performance_metrics (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    service_name VARCHAR(255) NOT NULL,
    metric_name VARCHAR(255) NOT NULL,
    metric_value DECIMAL(15,4) NOT NULL,
    metric_unit VARCHAR(50),
    tags JSONB,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_nodes_type_id ON codex.nodes(type_id);
CREATE INDEX IF NOT EXISTS idx_nodes_state ON codex.nodes(state);
CREATE INDEX IF NOT EXISTS idx_concepts_service_id ON codex.concepts(service_id);
CREATE INDEX IF NOT EXISTS idx_concepts_status ON codex.concepts(status);
CREATE INDEX IF NOT EXISTS idx_translations_concept_id ON codex.translations(concept_id);
CREATE INDEX IF NOT EXISTS idx_translations_languages ON codex.translations(source_language, target_language);
CREATE INDEX IF NOT EXISTS idx_contributions_user_id ON codex.contributions(user_id);
CREATE INDEX IF NOT EXISTS idx_contributions_concept_id ON codex.contributions(concept_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_action ON security.audit_logs(action);
CREATE INDEX IF NOT EXISTS idx_audit_logs_user_id ON security.audit_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON security.audit_logs(created_at);
CREATE INDEX IF NOT EXISTS idx_performance_metrics_service ON monitoring.performance_metrics(service_name);
CREATE INDEX IF NOT EXISTS idx_performance_metrics_timestamp ON monitoring.performance_metrics(timestamp);

-- Create full-text search indexes
CREATE INDEX IF NOT EXISTS idx_nodes_title_gin ON codex.nodes USING gin(to_tsvector('english', title));
CREATE INDEX IF NOT EXISTS idx_nodes_description_gin ON codex.nodes USING gin(to_tsvector('english', description));

-- Insert default admin user
INSERT INTO security.users (username, email, password_hash, role) 
VALUES ('admin', 'admin@livingcodex.com', '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'admin')
ON CONFLICT (username) DO NOTHING;

-- Insert default user
INSERT INTO security.users (username, email, password_hash, role) 
VALUES ('user', 'user@livingcodex.com', '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', 'user')
ON CONFLICT (username) DO NOTHING;

-- Create views for common queries
CREATE OR REPLACE VIEW codex.active_concepts AS
SELECT c.*, n.title, n.description
FROM codex.concepts c
LEFT JOIN codex.nodes n ON c.id = n.id
WHERE c.status = 'active';

CREATE OR REPLACE VIEW codex.user_contributions_summary AS
SELECT 
    u.username,
    u.email,
    COUNT(c.id) as total_contributions,
    COUNT(DISTINCT c.concept_id) as unique_concepts,
    MAX(c.created_at) as last_contribution
FROM security.users u
LEFT JOIN codex.contributions c ON u.id = c.user_id
GROUP BY u.id, u.username, u.email;

-- Grant permissions
GRANT USAGE ON SCHEMA codex TO postgres;
GRANT USAGE ON SCHEMA security TO postgres;
GRANT USAGE ON SCHEMA monitoring TO postgres;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA codex TO postgres;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA security TO postgres;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA monitoring TO postgres;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA codex TO postgres;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA security TO postgres;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA monitoring TO postgres;

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create additional indexes for better performance
CREATE INDEX IF NOT EXISTS idx_sites_user_status ON sites(user_id, status);
CREATE INDEX IF NOT EXISTS idx_sites_platform_created ON sites(platform, created_at);
CREATE INDEX IF NOT EXISTS idx_domains_site_type ON domains(site_id, type);
CREATE INDEX IF NOT EXISTS idx_deployments_site_status ON deployments(site_id, status);
CREATE INDEX IF NOT EXISTS idx_backups_site_created ON site_backups(site_id, created_at);

-- Create functions for automatic timestamps
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply triggers for updated_at columns
CREATE TRIGGER update_sites_updated_at BEFORE UPDATE ON sites
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_domains_updated_at BEFORE UPDATE ON domains
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_templates_updated_at BEFORE UPDATE ON templates
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
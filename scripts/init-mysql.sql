-- Create database for site management
CREATE DATABASE IF NOT EXISTS site_databases CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Create user for control panel (if not exists)
CREATE USER IF NOT EXISTS 'hypercontrol'@'%' IDENTIFIED BY 'temporary_password';
GRANT ALL PRIVILEGES ON site_databases.* TO 'hypercontrol'@'%';
FLUSH PRIVILEGES;

-- Enable general logging for debugging (optional)
SET GLOBAL general_log = 'ON';
SET GLOBAL general_log_file = '/var/log/mysql/general.log';
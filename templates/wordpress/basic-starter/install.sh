#!/bin/bash

# WordPress Basic Starter Installation Script
# This script sets up a WordPress site from the basic starter template

set -e

# Configuration variables
SITE_ID="${SITE_ID}"
SITE_DOMAIN="${SITE_DOMAIN}"
DATABASE_NAME="${DATABASE_NAME}"
DATABASE_USER="${DATABASE_USER}"
DATABASE_PASSWORD="${DATABASE_PASSWORD}"
DATABASE_HOST="${DATABASE_HOST}"
SITE_PATH="${SITE_PATH:-/var/www/html}"
SITE_TITLE="${SITE_TITLE:-My WordPress Site}"
SITE_DESCRIPTION="${SITE_DESCRIPTION:-A WordPress site built with Hyper Control Panel}"
ADMIN_EMAIL="${ADMIN_EMAIL}"
THEME_COLOR="${THEME_COLOR:-#1976d2}"

echo "Starting WordPress installation for site: $SITE_DOMAIN"

# Function to create wp-config.php
create_wp_config() {
    echo "Creating wp-config.php..."
    cat > "$SITE_PATH/wp-config.php" << EOF
<?php
/**
 * WordPress基础配置文件。
 *
 * 这个文件被安装程序用于自动生成 wp-config.php 配置文件，
 * 您可以不使用网站，您需要手动复制这个文件，
 * 并重命名为“wp-config.php”，然后填入相关信息。
 *
 * @package WordPress
 */

// ** MySQL 设置 - 具体信息来自您正在使用的主机 ** //
/** WordPress数据库的名称 */
define('DB_NAME', '$DATABASE_NAME');

/** MySQL数据库用户名 */
define('DB_USER', '$DATABASE_USER');

/** MySQL数据库密码 */
define('DB_PASSWORD', '$DATABASE_PASSWORD');

/** MySQL主机 */
define('DB_HOST', '$DATABASE_HOST');

/** 数据库字符集 */
define('DB_CHARSET', 'utf8mb4');

/** 数据库排序规则 */
define('DB_COLLATE', 'utf8mb4_unicode_ci');

/**#@+
 * 身份认证密钥设置。
 *
 * 您可以写一些随机字符，或者访问 {@link https://api.wordpress.org/secret-key/1.1/salt/ WordPress.org 密钥生成服务}
 * 生成所需的密钥。您可以对这些密钥进行更改，任何修改都会导致所有 cookies 失效。
 *
 * @since 2.6.0
 */
define('AUTH_KEY',         'put your unique phrase here');
define('SECURE_AUTH_KEY',  'put your unique phrase here');
define('LOGGED_IN_KEY',     'put your unique phrase here');
define('NONCE_KEY',        'put your unique phrase here');
define('AUTH_SALT',        'put your unique phrase here');
define('SECURE_AUTH_SALT', 'put your unique phrase here');
define('LOGGED_IN_SALT',   'put your unique phrase here');
define('NONCE_SALT',       'put your unique phrase here');

/**#@-*/

/**
 * WordPress数据表前缀。
 *
 * 如果您有在同一数据库内安装多个WordPress的需求，请为每个WordPress设置
 * 不同的数据表前缀。前缀名只能为数字、字母加下划线。
 */
\$table_prefix = 'wp_';

/**
 * 开发者专用：WordPress调试模式。
 *
 * 将这个值改为true，WordPress将显示所有用于开发时的提示。
 * 强烈建议插件开发者在开发环境中启用本功能。
 *
 * 要获取其他不能用于生产环境的提示，请参阅 WP_DEBUG_LOG 和 WP_DEBUG_DISPLAY。
 */
define('WP_DEBUG', false);

/**
 * WordPress URL设置
 */
define('WP_HOME',    'https://$SITE_DOMAIN');
define('WP_SITEURL', 'https://$SITE_DOMAIN');

/* 好了！请不要再继续编辑。请保存本文件。 */

/** WordPress目录的绝对路径。 */
if ( !defined('ABSPATH') )
    define('ABSPATH', dirname(__FILE__) . '/');

/** 设置WordPress变量和包含文件。 */
require_once(ABSPATH . 'wp-settings.php');
EOF
    echo "wp-config.php created successfully"
}

# Function to install WordPress database
install_wordpress() {
    echo "Installing WordPress database..."

    # Download WordPress CLI if not exists
    if [ ! -f "$SITE_PATH/wp-cli.phar" ]; then
        curl -o "$SITE_PATH/wp-cli.phar" https://raw.githubusercontent.com/wp-cli/builds/gh-pages/phar/wp-cli.phar
        chmod +x "$SITE_PATH/wp-cli.phar"
    fi

    # Install WordPress
    cd "$SITE_PATH"
    php wp-cli.phar core install --url="https://$SITE_DOMAIN" --title="$SITE_TITLE" --admin_user=admin --admin_password="$(openssl rand -base64 12)" --admin_email="$ADMIN_EMAIL" --skip-email

    echo "WordPress database installed successfully"
}

# Function to create basic theme
create_basic_theme() {
    echo "Creating basic theme..."

    THEME_DIR="$SITE_PATH/wp-content/themes/basic-starter"
    mkdir -p "$THEME_DIR"

    # Create style.css
    cat > "$THEME_DIR/style.css" << EOF
/*
Theme Name: Basic Starter
Theme URI: https://hypercontrolpanel.com
Author: Hyper Control Panel
Author URI: https://hypercontrolpanel.com
Description: A clean and minimal WordPress starter theme
Version: 1.0.0
License: GNU General Public License v2 or later
License URI: http://www.gnu.org/licenses/gpl-2.0.html
Text Domain: basic-starter
*/

:root {
    --primary-color: $THEME_COLOR;
    --text-color: #333;
    --bg-color: #fff;
    --border-color: #ddd;
}

body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
    line-height: 1.6;
    color: var(--text-color);
    background-color: var(--bg-color);
    margin: 0;
    padding: 0;
}

.container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 0 20px;
}

.header {
    background: var(--primary-color);
    color: white;
    padding: 1rem 0;
}

.header a {
    color: white;
    text-decoration: none;
}

.nav ul {
    list-style: none;
    padding: 0;
    margin: 0;
    display: flex;
}

.nav li {
    margin-right: 2rem;
}

.main {
    min-height: 60vh;
    padding: 2rem 0;
}

.footer {
    background: #333;
    color: white;
    text-align: center;
    padding: 2rem 0;
}

.btn {
    display: inline-block;
    background: var(--primary-color);
    color: white;
    padding: 0.75rem 1.5rem;
    text-decoration: none;
    border-radius: 4px;
    border: none;
    cursor: pointer;
}

.btn:hover {
    background: color(var(--primary-color) shade(10%));
}
EOF

    # Create index.php
    cat > "$THEME_DIR/index.php" << EOF
<?php get_header(); ?>

<div class="main">
    <div class="container">
        <?php if ( have_posts() ) : ?>
            <?php while ( have_posts() ) : the_post(); ?>
                <article <?php post_class(); ?>>
                    <h1><a href="<?php the_permalink(); ?>"><?php the_title(); ?></a></h1>
                    <div class="post-content">
                        <?php the_excerpt(); ?>
                    </div>
                    <div class="post-meta">
                        <?php echo get_the_date(); ?> | <?php the_author(); ?>
                    </div>
                </article>
            <?php endwhile; ?>

            <?php the_posts_pagination(); ?>
        <?php else : ?>
            <p><?php _e( 'Sorry, no posts matched your criteria.', 'basic-starter' ); ?></p>
        <?php endif; ?>
    </div>
</div>

<?php get_footer(); ?>
EOF

    # Create header.php
    cat > "$THEME_DIR/header.php" << EOF
<!DOCTYPE html>
<html <?php language_attributes(); ?>>
<head>
    <meta charset="<?php bloginfo( 'charset' ); ?>">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title><?php bloginfo( 'name' ); ?> <?php wp_title( '|', true, 'left' ); ?></title>
    <?php wp_head(); ?>
</head>
<body <?php body_class(); ?>>

<header class="header">
    <div class="container">
        <div style="display: flex; justify-content: space-between; align-items: center;">
            <h1 style="margin: 0;">
                <a href="<?php echo home_url( '/' ); ?>">
                    <?php bloginfo( 'name' ); ?>
                </a>
            </h1>
            <nav class="nav">
                <?php wp_nav_menu( array( 'theme_location' => 'primary', 'menu_class' => 'nav-ul' ) ); ?>
            </nav>
        </div>
    </div>
</header>
EOF

    # Create footer.php
    cat > "$THEME_DIR/footer.php" << EOF
<footer class="footer">
    <div class="container">
        <p>&copy; <?php echo date('Y'); ?> <?php bloginfo( 'name' ); ?>. All rights reserved.</p>
    </div>
</footer>

<?php wp_footer(); ?>
</body>
</html>
EOF

    # Create functions.php
    cat > "$THEME_DIR/functions.php" << EOF
<?php

// Theme setup
function basic_starter_setup() {
    // Add default posts and comments RSS feed links to head.
    add_theme_support( 'automatic-feed-links' );

    // Let WordPress manage the document title.
    add_theme_support( 'title-tag' );

    // Enable support for Post Thumbnails on posts and pages.
    add_theme_support( 'post-thumbnails' );

    // Register navigation menus
    register_nav_menus( array(
        'primary' => 'Primary Menu',
        'footer' => 'Footer Menu',
    ) );

    // Enable support for HTML5 markup.
    add_theme_support( 'html5', array(
        'search-form',
        'comment-form',
        'comment-list',
        'gallery',
        'caption',
    ) );
}
add_action( 'after_setup_theme', 'basic_starter_setup' );

// Enqueue scripts and styles
function basic_starter_scripts() {
    wp_enqueue_style( 'basic-starter-style', get_stylesheet_uri() );
}
add_action( 'wp_enqueue_scripts', 'basic_starter_scripts' );

// Register widget area
function basic_starter_widgets_init() {
    register_sidebar( array(
        'name'          => 'Sidebar',
        'id'            => 'sidebar-1',
        'before_widget' => '<div class="widget %2$s">',
        'after_widget'  => '</div>',
        'before_title'  => '<h3 class="widget-title">',
        'after_title'   => '</h3>',
    ) );
}
add_action( 'widgets_init', 'basic_starter_widgets_init' );

?>
EOF

    echo "Basic theme created successfully"
}

# Function to create default pages
create_default_pages() {
    echo "Creating default pages..."

    cd "$SITE_PATH"

    # Create Home page
    php wp-cli.phar post create --post_type=page --post_title='Home' --post_content='Welcome to your new WordPress site! This is your homepage where you can showcase your most important content.' --post_status=publish

    # Create About page
    php wp-cli.phar post create --post_type=page --post_title='About' --post_content='Learn more about us, our mission, and what we do.' --post_status=publish

    # Create Contact page
    php wp-cli.phar post create --post_type=page --post_title='Contact' --post_content='Get in touch with us through our contact form or contact information.' --post_status=publish

    # Create Blog page
    php wp-cli.phar post create --post_type=page --post_title='Blog' --post_content='Read our latest articles and updates.' --post_status=publish

    # Set the home page as front page
    HOME_ID=$(php wp-cli.phar post list --post_type=page --name=home --field=ID)
    BLOG_ID=$(php wp-cli.phar post list --post_type=page --name=blog --field=ID)

    php wp-cli.phar option update show_on_front 'page'
    php wp-cli.phar option update page_on_front $HOME_ID
    php wp-cli.phar option update page_for_posts $BLOG_ID

    echo "Default pages created successfully"
}

# Function to configure WordPress settings
configure_wordpress() {
    echo "Configuring WordPress settings..."

    cd "$SITE_PATH"

    # Set site description
    php wp-cli.phar option update blogdescription "$SITE_DESCRIPTION"

    # Set permalink structure
    php wp-cli.phar rewrite structure '/%postname%/'

    # Delete default posts and pages
    php wp-cli.phar post delete 1 --force  # Delete "Hello World" post
    php wp-cli.phar post delete 2 --force  # Delete "Sample Page"

    # Delete default plugins
    php wp-cli.phar plugin delete hello.php akismet

    # Activate basic starter theme
    php wp-cli.phar theme activate basic-starter

    echo "WordPress settings configured successfully"
}

# Main installation process
main() {
    echo "Starting WordPress installation process..."

    # Check if WordPress is already installed
    if [ -f "$SITE_PATH/wp-config.php" ]; then
        echo "WordPress appears to be already installed. Skipping installation."
        return 0
    fi

    # Execute installation steps
    create_wp_config
    install_wordpress
    create_basic_theme
    create_default_pages
    configure_wordpress

    # Set proper permissions
    chown -R www-data:www-data "$SITE_PATH"
    find "$SITE_PATH" -type d -exec chmod 755 {} \;
    find "$SITE_PATH" -type f -exec chmod 644 {} \;

    echo "WordPress installation completed successfully!"
    echo "Site URL: https://$SITE_DOMAIN"
    echo "Admin URL: https://$SITE_DOMAIN/wp-admin"
}

# Execute main function
main "$@"
// User types
export interface User {
  id: string;
  email: string;
  firstName?: string;
  lastName?: string;
  createdAt: string;
  lastLoginAt?: string;
  isActive: boolean;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiration: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName?: string;
  lastName?: string;
}

// Site types
export interface Site {
  id: string;
  name: string;
  description?: string;
  domain: string;
  platform: string;
  template: string;
  status: 'creating' | 'active' | 'suspended' | 'error' | 'deleting';
  storageLimitGB: number;
  storageUsedMB: number;
  createdAt: string;
  updatedAt: string;
  lastBackupAt?: string;
  domains: Domain[];
  databases: Database[];
  url: string;
  isAccessible: boolean;
}

export interface SiteList {
  id: string;
  name: string;
  domain: string;
  platform: string;
  status: string;
  createdAt: string;
  storageUsedMB: number;
  storageLimitGB: number;
  url: string;
  isAccessible: boolean;
  domainCount: number;
  hasSsl: boolean;
}

export interface CreateSiteRequest {
  name: string;
  description?: string;
  domain: string;
  platform: string;
  template: string;
  storageLimitGB?: number;
  customDomains?: string[];
}

export interface UpdateSiteRequest {
  name?: string;
  description?: string;
  storageLimitGB?: number;
}

// Domain types
export interface Domain {
  id: string;
  siteId: string;
  domainName: string;
  type: 'subdomain' | 'custom';
  sslEnabled: boolean;
  sslExpiresAt?: string;
  dnsVerified: boolean;
  isPrimary: boolean;
  createdAt: string;
  daysUntilSslExpiry: number;
  sslStatus: string;
}

export interface AddDomainRequest {
  domainName: string;
  type: 'subdomain' | 'custom';
  isPrimary?: boolean;
}

// Database types
export interface Database {
  id: string;
  databaseName: string;
  username: string;
  host: string;
  port: number;
  databaseType: string;
  createdAt: string;
}

// Template types
export interface Template {
  id: string;
  name: string;
  description?: string;
  platform: string;
  category: string;
  screenshotUrl?: string;
  previewUrl?: string;
  isFeatured: boolean;
  sortOrder: number;
  createdAt: string;
}

export interface TemplateFilter {
  platform?: string;
  category?: string;
  featuredOnly?: boolean;
  page?: number;
  pageSize?: number;
}

// Deployment types
export interface Deployment {
  id: string;
  siteId: string;
  type: string;
  status: 'pending' | 'running' | 'completed' | 'failed';
  message?: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  duration: string;
  isRunning: boolean;
}

// Backup types
export interface Backup {
  id: string;
  siteId: string;
  fileName: string;
  fileSizeBytes: number;
  fileSizeDisplay: string;
  type: 'manual' | 'automatic';
  description?: string;
  createdAt: string;
  expiresAt?: string;
  isExpired: boolean;
}

export interface CreateBackupRequest {
  description?: string;
  includeDatabase?: boolean;
  includeFiles?: boolean;
}

// File management types
export interface FileItem {
  name: string;
  path: string;
  isDirectory: boolean;
  size: number;
  sizeDisplay: string;
  modifiedAt: string;
  permissions: string;
  extension: string;
}

export interface FileBrowserResponse {
  currentPath: string;
  items: FileItem[];
  breadcrumbs: string[];
  totalSize: number;
  totalSizeDisplay: string;
}

export interface FileBrowserRequest {
  path: string;
  showHidden?: boolean;
}

// Statistics types
export interface SiteStats {
  totalSites: number;
  activeSites: number;
  suspendedSites: number;
  totalStorageUsedMB: number;
  totalDomains: number;
  domainsWithSsl: number;
  totalDatabases: number;
  lastActivity: string;
}

export interface DashboardStats {
  siteStats: SiteStats;
  recentActivity: RecentActivity[];
  recentSites: SiteList[];
  popularTemplates: Template[];
}

export interface RecentActivity {
  type: string;
  description: string;
  entityName: string;
  entityId: string;
  createdAt: string;
  relativeTime: string;
}

// API response types
export interface ApiResponse<T = any> {
  data?: T;
  error?: string;
  message?: string;
  success: boolean;
}

export interface PaginatedResponse<T> {
  data: T[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

// Form types
export interface FormField {
  name: string;
  label: string;
  type: 'text' | 'email' | 'password' | 'select' | 'textarea' | 'checkbox';
  required?: boolean;
  options?: { value: string; label: string }[];
  placeholder?: string;
  validation?: any;
}

export interface FormState {
  isSubmitting: boolean;
  errors: Record<string, string>;
  touched: Record<string, boolean>;
}

// UI State types
export interface LoadingState {
  [key: string]: boolean;
}

export interface ErrorState {
  [key: string]: string | null;
}

// Navigation types
export interface NavigationItem {
  path: string;
  label: string;
  icon?: string;
  children?: NavigationItem[];
  badge?: string | number;
}

// Modal types
export interface ModalState {
  isOpen: boolean;
  title?: string;
  content?: React.ReactNode;
  size?: 'small' | 'medium' | 'large';
}

// Theme types
export interface ThemeConfig {
  mode: 'light' | 'dark';
  primaryColor: string;
  secondaryColor: string;
}

export interface AppSettings {
  theme: ThemeConfig;
  language: string;
  notifications: boolean;
  autoRefresh: boolean;
}
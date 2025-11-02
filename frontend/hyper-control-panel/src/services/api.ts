import axios, { AxiosInstance, AxiosResponse } from 'axios';
import { AuthResponse, LoginRequest, RegisterRequest, ApiResponse } from '../types';

class ApiService {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor to add auth token
    this.client.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Response interceptor to handle auth errors
    this.client.interceptors.response.use(
      (response) => response,
      async (error) => {
        if (error.response?.status === 401) {
          // Token expired or invalid
          const refreshToken = localStorage.getItem('refreshToken');
          if (refreshToken) {
            try {
              const response = await this.refreshToken(refreshToken);
              localStorage.setItem('token', response.token);
              localStorage.setItem('refreshToken', response.refreshToken);

              // Retry the original request
              error.config.headers.Authorization = `Bearer ${response.token}`;
              return this.client.request(error.config);
            } catch (refreshError) {
              // Refresh token failed, logout user
              this.logout();
              window.location.href = '/login';
            }
          } else {
            this.logout();
            window.location.href = '/login';
          }
        }
        return Promise.reject(error);
      }
    );
  }

  // Authentication endpoints
  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await this.client.post<ApiResponse<AuthResponse>>('/auth/login', data);
    return response.data.data!;
  }

  async register(data: RegisterRequest): Promise<AuthResponse> {
    const response = await this.client.post<ApiResponse<AuthResponse>>('/auth/register', data);
    return response.data.data!;
  }

  async logout(): Promise<void> {
    try {
      await this.client.post('/auth/logout');
    } finally {
      localStorage.removeItem('token');
      localStorage.removeItem('refreshToken');
      localStorage.removeItem('user');
    }
  }

  async refreshToken(refreshToken: string): Promise<AuthResponse> {
    const response = await this.client.post<ApiResponse<AuthResponse>>('/auth/refresh', {
      token: localStorage.getItem('token'),
      refreshToken,
    });
    return response.data.data!;
  }

  async getProfile() {
    const response = await this.client.get<ApiResponse>('/auth/profile');
    return response.data.data;
  }

  async updateProfile(data: any) {
    const response = await this.client.put<ApiResponse>('/auth/profile', data);
    return response.data.data;
  }

  async changePassword(data: any) {
    const response = await this.client.post<ApiResponse>('/auth/change-password', data);
    return response.data;
  }

  // Sites endpoints
  async getSites() {
    const response = await this.client.get<ApiResponse>('/sites');
    return response.data.data;
  }

  async getSite(id: string) {
    const response = await this.client.get<ApiResponse>(`/sites/${id}`);
    return response.data.data;
  }

  async createSite(data: any) {
    const response = await this.client.post<ApiResponse>('/sites', data);
    return response.data.data;
  }

  async updateSite(id: string, data: any) {
    const response = await this.client.put<ApiResponse>(`/sites/${id}`, data);
    return response.data.data;
  }

  async deleteSite(id: string) {
    const response = await this.client.delete<ApiResponse>(`/sites/${id}`);
    return response.data;
  }

  async getSiteStats() {
    const response = await this.client.get<ApiResponse>('/sites/stats');
    return response.data.data;
  }

  async createSiteBackup(id: string, data: any) {
    const response = await this.client.post<ApiResponse>(`/sites/${id}/backup`, data);
    return response.data.data;
  }

  async getSiteBackups(id: string) {
    const response = await this.client.get<ApiResponse>(`/sites/${id}/backups`);
    return response.data.data;
  }

  async cloneSite(id: string, data: any) {
    const response = await this.client.post<ApiResponse>(`/sites/${id}/clone`, data);
    return response.data.data;
  }

  async getSiteDeployments(id: string) {
    const response = await this.client.get<ApiResponse>(`/sites/${id}/deployments`);
    return response.data.data;
  }

  async restartSite(id: string) {
    const response = await this.client.post<ApiResponse>(`/sites/${id}/restart`);
    return response.data;
  }

  // Domains endpoints
  async addDomain(siteId: string, data: any) {
    const response = await this.client.post<ApiResponse>(`/sites/${siteId}/domains`, data);
    return response.data.data;
  }

  async updateDomain(domainId: string, data: any) {
    const response = await this.client.put<ApiResponse>(`/domains/${domainId}`, data);
    return response.data.data;
  }

  async deleteDomain(domainId: string) {
    const response = await this.client.delete<ApiResponse>(`/domains/${domainId}`);
    return response.data;
  }

  async verifyDomainSsl(domainId: string) {
    const response = await this.client.post<ApiResponse>(`/domains/${domainId}/verify-ssl`);
    return response.data;
  }

  // Templates endpoints
  async getTemplates(params?: any) {
    const response = await this.client.get<ApiResponse>('/templates', { params });
    return response.data.data;
  }

  async getTemplate(id: string) {
    const response = await this.client.get<ApiResponse>(`/templates/${id}`);
    return response.data.data;
  }

  async installTemplate(siteId: string, templateId: string) {
    const response = await this.client.post<ApiResponse>(`/sites/${siteId}/install-template`, {
      templateId,
    });
    return response.data;
  }

  // File management endpoints
  async browseFiles(siteId: string, params: any) {
    const response = await this.client.get<ApiResponse>(`/sites/${siteId}/files`, { params });
    return response.data.data;
  }

  async createDirectory(siteId: string, data: any) {
    const response = await this.client.post<ApiResponse>(`/sites/${siteId}/files/directory`, data);
    return response.data;
  }

  async uploadFile(siteId: string, path: string, file: File) {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('path', path);

    const response = await this.client.post<ApiResponse>(`/sites/${siteId}/files/upload`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  }

  async downloadFile(siteId: string, path: string) {
    const response = await this.client.get(`/sites/${siteId}/files/download`, {
      params: { path },
      responseType: 'blob',
    });
    return response.data;
  }

  async deleteFile(siteId: string, path: string) {
    const response = await this.client.delete<ApiResponse>(`/sites/${siteId}/files`, {
      params: { path },
    });
    return response.data;
  }

  async readFile(siteId: string, path: string) {
    const response = await this.client.get<ApiResponse>(`/sites/${siteId}/files/content`, {
      params: { path },
    });
    return response.data.data;
  }

  async writeFile(siteId: string, path: string, content: string) {
    const response = await this.client.post<ApiResponse>(`/sites/${siteId}/files/content`, {
      path,
      content,
    });
    return response.data;
  }

  // Utility methods
  getAxiosInstance(): AxiosInstance {
    return this.client;
  }

  setAuthToken(token: string): void {
    localStorage.setItem('token', token);
  }

  getAuthToken(): string | null {
    return localStorage.getItem('token');
  }

  removeAuthToken(): void {
    localStorage.removeItem('token');
  }

  setRefreshToken(token: string): void {
    localStorage.setItem('refreshToken', token);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  removeRefreshToken(): void {
    localStorage.removeItem('refreshToken');
  }

  isAuthenticated(): boolean {
    return !!this.getAuthToken();
  }
}

// Create singleton instance
const apiService = new ApiService();

export default apiService;
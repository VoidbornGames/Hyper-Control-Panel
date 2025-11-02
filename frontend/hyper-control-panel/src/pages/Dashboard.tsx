import React from 'react';
import {
  Box,
  Grid,
  Card,
  CardContent,
  Typography,
  Button,
  Paper,
  LinearProgress,
} from '@mui/material';
import {
  Add,
  Language,
  Storage,
  Security,
  Backup,
  Settings,
} from '@mui/icons-material';

import { useQuery } from 'react-query';
import { DashboardStats } from '../types';
import apiService from '../services/api';
import StatCard from '../components/StatCard';
import RecentSites from '../components/RecentSites';
import RecentActivity from '../components/RecentActivity';

const Dashboard: React.FC = () => {
  const {
    data: dashboardStats,
    isLoading,
    error,
  } = useQuery('dashboardStats', () => apiService.getSiteStats());

  if (isLoading) {
    return (
      <Box sx={{ width: '100%', mt: 2 }}>
        <LinearProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography color="error">
          Failed to load dashboard data. Please try again.
        </Typography>
      </Box>
    );
  }

  const stats = dashboardStats || {
    totalSites: 0,
    activeSites: 0,
    suspendedSites: 0,
    totalStorageUsedMB: 0,
    totalDomains: 0,
    domainsWithSsl: 0,
    totalDatabases: 0,
    lastActivity: new Date().toISOString(),
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom fontWeight="bold">
          Dashboard
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Welcome back! Here's an overview of your websites.
        </Typography>
      </Box>

      {/* Quick Actions */}
      <Box sx={{ mb: 4 }}>
        <Button
          variant="contained"
          size="large"
          startIcon={<Add />}
          href="/sites/new"
          sx={{ mr: 2 }}
        >
          Create New Site
        </Button>
        <Button
          variant="outlined"
          size="large"
          startIcon={<Backup />}
          href="/backups"
        >
          Manage Backups
        </Button>
      </Box>

      {/* Stats Grid */}
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Total Sites"
            value={stats.totalSites}
            icon={<Language />}
            color="#1976d2"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Active Sites"
            value={stats.activeSites}
            icon={<Language />}
            color="#2e7d32"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="Storage Used"
            value={`${(stats.totalStorageUsedMB / 1024).toFixed(1)} GB`}
            icon={<Storage />}
            color="#ed6c02"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <StatCard
            title="SSL Certificates"
            value={stats.domainsWithSsl}
            icon={<Security />}
            color="#9c27b0"
          />
        </Grid>
      </Grid>

      {/* Main Content Grid */}
      <Grid container spacing={3}>
        {/* Recent Sites */}
        <Grid item xs={12} lg={8}>
          <Paper sx={{ p: 3, height: '100%' }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
              <Typography variant="h6" component="h2" fontWeight="bold">
                Recent Sites
              </Typography>
              <Button variant="text" href="/sites">
                View All
              </Button>
            </Box>
            <RecentSites />
          </Paper>
        </Grid>

        {/* Quick Actions & Recent Activity */}
        <Grid item xs={12} lg={4}>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
            {/* Quick Actions */}
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" component="h2" fontWeight="bold" gutterBottom>
                Quick Actions
              </Typography>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <Button
                  variant="outlined"
                  startIcon={<Add />}
                  fullWidth
                  href="/sites/new"
                >
                  Create Site
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<Language />}
                  fullWidth
                  href="/domains"
                >
                  Manage Domains
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<Backup />}
                  fullWidth
                  href="/backups"
                >
                  Backups
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<Settings />}
                  fullWidth
                  href="/settings"
                >
                  Settings
                </Button>
              </Box>
            </Paper>

            {/* Recent Activity */}
            <Paper sx={{ p: 3, flex: 1 }}>
              <Typography variant="h6" component="h2" fontWeight="bold" gutterBottom>
                Recent Activity
              </Typography>
              <RecentActivity />
            </Paper>
          </Box>
        </Grid>
      </Grid>
    </Box>
  );
};

export default Dashboard;
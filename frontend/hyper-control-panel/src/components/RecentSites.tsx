import React from 'react';
import { Box, Typography, List, ListItem, ListItemText, Chip } from '@mui/material';
import { Link } from 'react-router-dom';

const RecentSites: React.FC = () => {
  // This would be populated with actual data from the API
  const recentSites = [
    { id: '1', name: 'My Blog', domain: 'blog.example.com', status: 'active' },
    { id: '2', name: 'Business Site', domain: 'business.example.com', status: 'active' },
    { id: '3', name: 'Portfolio', domain: 'portfolio.example.com', status: 'creating' },
  ];

  return (
    <Box>
      {recentSites.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ py: 4, textAlign: 'center' }}>
          No sites yet. Create your first site to get started!
        </Typography>
      ) : (
        <List>
          {recentSites.map((site) => (
            <ListItem key={site.id} divider>
              <ListItemText
                primary={
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Link
                      to={`/sites/${site.id}`}
                      style={{
                        textDecoration: 'none',
                        color: 'inherit',
                        fontWeight: 500,
                      }}
                    >
                      {site.name}
                    </Link>
                    <Chip
                      label={site.status}
                      size="small"
                      color={site.status === 'active' ? 'success' : 'warning'}
                    />
                  </Box>
                }
                secondary={site.domain}
              />
            </ListItem>
          ))}
        </List>
      )}
    </Box>
  );
};

export default RecentSites;
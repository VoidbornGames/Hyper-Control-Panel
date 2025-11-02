import React from 'react';
import { Box, Typography, List, ListItem, ListItemText } from '@mui/material';

const RecentActivity: React.FC = () => {
  // This would be populated with actual data from the API
  const activities = [
    { type: 'site_created', description: 'Created site "My Blog"', time: '2 hours ago' },
    { type: 'backup_created', description: 'Created backup for Business Site', time: '5 hours ago' },
    { type: 'domain_added', description: 'Added domain portfolio.example.com', time: '1 day ago' },
  ];

  return (
    <Box>
      {activities.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ py: 2, textAlign: 'center' }}>
          No recent activity
        </Typography>
      ) : (
        <List sx={{ p: 0 }}>
          {activities.map((activity, index) => (
            <ListItem key={index} sx={{ px: 0, py: 1 }}>
              <ListItemText
                primary={activity.description}
                secondary={activity.time}
                primaryTypographyProps={{
                  variant: 'body2',
                  sx: { fontWeight: 500 }
                }}
                secondaryTypographyProps={{
                  variant: 'caption'
                }}
              />
            </ListItem>
          ))}
        </List>
      )}
    </Box>
  );
};

export default RecentActivity;
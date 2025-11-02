import React from 'react';
import { Box, Typography, Paper } from '@mui/material';

const SiteDetail: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          SiteDetail
        </Typography>
        <Typography>
          SiteDetail functionality will be implemented here.
        </Typography>
      </Paper>
    </Box>
  );
};

export default SiteDetail;

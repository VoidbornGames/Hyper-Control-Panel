import React from 'react';
import { Box, Typography, Paper } from '@mui/material';

const Sites: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          Sites
        </Typography>
        <Typography>
          Sites functionality will be implemented here.
        </Typography>
      </Paper>
    </Box>
  );
};

export default Sites;

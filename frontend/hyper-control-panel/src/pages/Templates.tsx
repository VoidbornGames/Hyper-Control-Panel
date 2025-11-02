import React from 'react';
import { Box, Typography, Paper } from '@mui/material';

const Templates: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          Templates
        </Typography>
        <Typography>
          Templates functionality will be implemented here.
        </Typography>
      </Paper>
    </Box>
  );
};

export default Templates;

import React from 'react';
import { Box, Typography, Paper } from '@mui/material';

const Files: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          Files
        </Typography>
        <Typography>
          Files functionality will be implemented here.
        </Typography>
      </Paper>
    </Box>
  );
};

export default Files;

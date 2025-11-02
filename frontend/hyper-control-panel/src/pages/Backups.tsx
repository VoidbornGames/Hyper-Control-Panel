import React from 'react';
import { Box, Typography, Paper } from '@mui/material';

const Backups: React.FC = () => {
  return (
    <Box sx={{ p: 3 }}>
      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          Backups
        </Typography>
        <Typography>
          Backups functionality will be implemented here.
        </Typography>
      </Paper>
    </Box>
  );
};

export default Backups;

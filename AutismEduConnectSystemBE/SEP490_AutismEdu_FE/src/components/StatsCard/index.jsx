import React from 'react';
import { Paper, Box, Typography } from '@mui/material';

const StatCard = ({ label, value, color, icon }) => {
    return (
        <Paper
            elevation={3}
            sx={{
                display: 'flex',
                alignItems: 'center',
                p: 2,
                bgcolor: color,
                borderRadius: '10px',
            }}
        >
            <Box sx={{ mr: 2, display: 'flex', alignItems: 'center' }}>
                {icon}
            </Box>
            <Box>
                <Typography variant="subtitle2">
                    {label}
                </Typography>
                <Typography variant="h5" sx={{ my: 1 }}>
                    {value}
                </Typography>
            </Box>
        </Paper>
    );
};

export default StatCard;
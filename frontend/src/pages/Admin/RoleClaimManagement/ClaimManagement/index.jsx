import { Box, Button, FormControl, InputLabel, MenuItem, Select, TextField, Typography } from '@mui/material'
import ClaimTable from './ClaimTable'
import { useState } from 'react';
import ClaimModal from '../RoleClaimModal/ClaimModal';

function ClaimManagement() {
    const [claim, setClaim] = useState('');

    const handleChange = (event) => {
        setClaim(event.target.value);
    };
    return (
        <Box sx={{
            width: "100%", bgcolor: "white", p: "20px",
            borderRadius: "10px",
            boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px"
        }}>
            <Typography variant='h6'>Claims</Typography>
            <Box sx={{
                width: "100%",
                display: "flex",
                justifyContent: "space-between",
                gap: 2,
                marginTop: "30px"
            }}>
                <Box sx={{ display: "flex", gap: 3 }}>
                    <FormControl size='small' sx={{ width: "120px" }}>
                        <InputLabel id="type-claim">Type</InputLabel>
                        <Select
                            labelId="type-claim"
                            id="type-claim-select"
                            value={claim}
                            label="Type"
                            onChange={handleChange}
                        >
                            <MenuItem value={10}>Create</MenuItem>
                            <MenuItem value={20}>View</MenuItem>
                            <MenuItem value={30}>Edit</MenuItem>
                            <MenuItem value={30}>Delete</MenuItem>
                        </Select>
                    </FormControl>
                    <TextField size='small' id="outlined-basic" label="Search claim" variant="outlined"
                        sx={{
                            width: "300px"
                        }} />
                </Box>
                <ClaimModal />
            </Box>
            <Box>
                <ClaimTable />
            </Box>
        </Box>
    )
}

export default ClaimManagement

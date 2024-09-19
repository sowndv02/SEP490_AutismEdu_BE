import { Box, Button, FormControl, InputLabel, MenuItem, Select, TextField, Typography } from '@mui/material'
import ClaimTable from './ClaimTable'
<<<<<<< HEAD
import { useState } from 'react';
import ClaimModal from '../RoleClaimModal/ClaimModal';

function ClaimManagement() {
    const [claim, setClaim] = useState('');

=======
import { useEffect, useState } from 'react';
import ClaimModal from '../RoleClaimModal/ClaimModal';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';

function ClaimManagement() {
    const [claim, setClaim] = useState('');
    useEffect(() => {
        services.ClaimManagementAPI.getClaims((res) => {
            console.log(res);
        }, (err) => {
            console.log(err);
        })
    }, [])
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    const handleChange = (event) => {
        setClaim(event.target.value);
    };
    return (
        <Box sx={{
            width: "100%", bgcolor: "white", p: "20px",
            borderRadius: "10px",
<<<<<<< HEAD
            boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px"
=======
            boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px",
            position:"relative"
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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

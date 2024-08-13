import { Box, Button, FormControl, InputLabel, MenuItem, Select, TextField, Typography } from '@mui/material'
import { useState } from 'react';
import AddIcon from '@mui/icons-material/Add';
import RoleTable from './RoleTable';
import RoleModal from '../RoleClaimModal/RoleModal';
function RoleManagement() {
    const [role, setRole] = useState('');

    const handleChange = (event) => {
        setRole(event.target.value);
    };
    return (
        <Box sx={{
            width: "100%", bgcolor: "white", p: "20px",
            borderRadius: "10px",
            boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px",
            marginTop: "30px"
        }}>
            <Typography variant='h6'>Roles</Typography>
            <Box sx={{
                width: "100%",
                display: "flex",
                justifyContent: "space-between",
                gap: 2,
                marginTop: "30px"
            }}>
                <FormControl size='small' sx={{ width: "180px" }}>
                    <InputLabel id="type-claim">Role</InputLabel>
                    <Select
                        labelId="type-claim"
                        id="type-claim-select"
                        value={role}
                        label="Type"
                        onChange={handleChange}
                    >
                        <MenuItem value={10}>Admin</MenuItem>
                        <MenuItem value={20}>Customer</MenuItem>
                    </Select>
                </FormControl>
                <RoleModal />
            </Box>
            <Box>
                <RoleTable />
            </Box>
        </Box>
    )
}

export default RoleManagement

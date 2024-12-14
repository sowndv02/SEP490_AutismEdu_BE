import { Box, Button, FormControl, InputLabel, MenuItem, Select, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react';
import AddIcon from '@mui/icons-material/Add';
import RoleTable from './RoleTable';
import RoleModal from '../RoleClaimModal/RoleModal';
import services from '~/plugins/services';
function RoleManagement() {
    const [roles, setRoles] = useState([]);
    const [status, setStatus] = useState(false);
    useEffect(() => {
        handleGetRoles();
    }, [status]);
    const handleGetRoles = async () => {
        try {
            await services.RoleManagementAPI.getRoles((res) => {
                setRoles(res.result);
            }, (err) => {
                console.log(err);
            });
        } catch (error) {
            console.log(error);
        }
    }

    return (
        <Box sx={{
            width: "100%", bgcolor: "white", p: "20px",
            borderRadius: "10px",
            boxShadow: "rgba(0, 0, 0, 0.24) 0px 3px 8px",
            marginTop: "30px"
        }}>
            <Typography variant='h5'>Danh sách các vai trò</Typography>
            <Box sx={{
                width: "100%",
                display: "flex",
                justifyContent: "end",
                gap: 2,
                marginTop: "30px"
            }}>

                <RoleModal roles={roles} setRoles={setRoles} handleGetRoles={handleGetRoles} />
            </Box>
            <Box>
                <RoleTable roles={roles} setRoles={setRoles} />
            </Box>
        </Box>
    )
}

export default RoleManagement

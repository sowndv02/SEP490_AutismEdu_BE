import { Box, Button, FormControl, InputLabel, MenuItem, Modal, Select, Typography } from '@mui/material';
import { useState } from 'react';
import DeleteRoleDialog from './DeleteRoleDialog';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 600,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4,
};
function UserRoleModal({ handleCloseMenu }) {
    const [open, setOpen] = useState(false);
    const [claim, setClaim] = useState('');
    const listClaim = [
        "Admin",
        "Customer"
    ]

    const listMyClaim = [
        "Admin"
    ]
    const handleOpen = () => {
        setOpen(true)
    };
    const handleClose = () => {
        handleCloseMenu()
        setOpen(false);
    }

    const handleChange = (event) => {
        setClaim(event.target.value);
    };
    return (
        <div>
            <MenuItem onClick={handleOpen}>Manage Role</MenuItem>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style} >
                    <Typography id="modal-modal-title" variant="h6" component="h2">
                        Roles of Khai Dao
                    </Typography>
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }} mt="20px">
                        <FormControl size="small" sx={{ width: "80%" }}>
                            <InputLabel id="demo-simple-select-label">Roles</InputLabel>
                            <Select
                                labelId="demo-simple-select-label"
                                id="demo-simple-select"
                                value={claim}
                                label="Claim"
                                onChange={handleChange}
                            >
                                {listClaim.map((l, index) => {
                                    return (
                                        <MenuItem key={index} value={10}>{l}</MenuItem>
                                    )
                                })}
                            </Select>
                        </FormControl>
                        <Button variant='contained' sx={{ alignSelf: "end", height: "40px", fontSize: "11px" }}>Add Role</Button>
                    </Box>
                    <Box mt="20px">
                        {
                            listMyClaim.map((l, index) => {
                                return (
                                    <Box key={index} sx={{
                                        display: "flex", alignItems: "center", justifyContent: "space-between",
                                        height: "60px",
                                        '&:hover': {
                                            bgcolor: "#f7f7f9"
                                        },
                                        px: "20px",
                                        py: "10px",
                                    }}>
                                        <Box>
                                            {l}
                                        </Box>
                                        <DeleteRoleDialog />
                                    </Box>
                                )
                            })
                        }
                    </Box>
                </Box>
            </Modal>
        </div>
    )
}

export default UserRoleModal

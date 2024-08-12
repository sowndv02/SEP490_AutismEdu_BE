import { Box, Button, FormControl, IconButton, InputLabel, MenuItem, Modal, Select, Typography } from '@mui/material';
import React, { useState } from 'react'
import RemoveIcon from '@mui/icons-material/Remove';
import DeleteClaimDialog from './DeleteClaimDialog';
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
function UserClaimModal() {
    const [open, setOpen] = useState(false);
    const [claim, setClaim] = useState('');
    const listClaim = [
        {
            type: "Create",
            value: "User information"
        },
        {
            type: "View",
            value: "User information"
        },
        {
            type: "Delete",
            value: "User information"
        },
        {
            type: "Edit",
            value: "User information"
        },
    ]

    const listMyClaim = [
        {
            type: "Create",
            value: "User information"
        },
        {
            type: "View",
            value: "User information"
        },
        {
            type: "Delete",
            value: "User information"
        },
        {
            type: "Edit",
            value: "User information"
        },
    ]
    const handleOpen = () => {
        setOpen(true)
    };
    const handleClose = () => setOpen(false);

    const handleChange = (event) => {
        setClaim(event.target.value);
    };
    return (
        <div>
            <MenuItem onClick={handleOpen}>Manage Claim</MenuItem>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style} >
                    <Typography id="modal-modal-title" variant="h6" component="h2">
                        Claim of Khai Dao
                    </Typography>
                    <Box sx={{ display: "flex", alignItems: "center", gap: 1 }} mt="20px">
                        <FormControl size="small" sx={{ width: "80%" }}>
                            <InputLabel id="demo-simple-select-label">Claim</InputLabel>
                            <Select
                                labelId="demo-simple-select-label"
                                id="demo-simple-select"
                                value={claim}
                                label="Claim"
                                onChange={handleChange}
                            >
                                {listClaim.map((l, index) => {
                                    return (
                                        <MenuItem key={index} value={10}>{l.type} - {l.value}</MenuItem>
                                    )
                                })}
                            </Select>
                        </FormControl>
                        <Button variant='contained' sx={{ alignSelf: "end", height: "40px", fontSize: "11px" }}>Add Claim</Button>
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
                                            {l.type} - {l.value}
                                        </Box>
                                        <DeleteClaimDialog />
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

export default UserClaimModal

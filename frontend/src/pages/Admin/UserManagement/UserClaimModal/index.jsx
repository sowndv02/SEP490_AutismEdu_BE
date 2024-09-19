<<<<<<< HEAD
import { Box, Button, FormControl, IconButton, InputLabel, MenuItem, Modal, Select, Typography } from '@mui/material';
import React, { useState } from 'react'
import RemoveIcon from '@mui/icons-material/Remove';
import DeleteClaimDialog from './DeleteClaimDialog';
=======
import { Box, MenuItem, Modal, Tab, Tabs, Typography } from '@mui/material';
import { useState } from 'react';
import ClaimTable from './ClaimTable';
import MyClaim from './MyClaim';
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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
<<<<<<< HEAD
function UserClaimModal({ handleCloseMenu }) {
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
=======
function UserClaimModal({ handleCloseMenu, currentUser }) {
    const [open, setOpen] = useState(false);
    const [tab, setTab] = useState(0);
    const [claims, setClaims] = useState(null);
    const [pagination, setPagination] = useState(null);
    const [selected, setSelected] = useState([]);
    const [currentPage, setCurrentPage] = useState(1)

>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    const handleOpen = () => {
        setOpen(true)
    };
    const handleClose = () => {
        handleCloseMenu()
        setOpen(false);
    }
<<<<<<< HEAD
    const handleChange = (event) => {
        setClaim(event.target.value);
    };
    return (
        <div>
            <MenuItem onClick={handleOpen}>Manage Claim</MenuItem>
=======

    const handleChangeTab = (event, newValue) => {
        setTab(newValue);
    };
    return (
        <div>
            <MenuItem onClick={() => { handleOpen(); }}>Manage Claim</MenuItem>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style} >
                    <Typography id="modal-modal-title" variant="h6" component="h2">
<<<<<<< HEAD
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
=======
                        Claim of {currentUser.fullName}
                    </Typography>
                    <Tabs value={tab} onChange={handleChangeTab} aria-label="basic tabs example">
                        <Tab label={`${currentUser.fullName}'s Claims`} />
                        <Tab label="Add Claims" />
                    </Tabs>

                    {
                        tab === 0 ? <MyClaim currentUser={currentUser} /> : <ClaimTable claims={claims} setClaims={setClaims} pagination={pagination}
                            setPagination={setPagination}
                            userId={currentUser.id}
                            selected={selected}
                            setSelected={setSelected}
                            setTab={setTab}
                            currentPage={currentPage}
                            setCurrentPage={setCurrentPage}
                        />
                    }
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                </Box>
            </Modal>
        </div>
    )
}

export default UserClaimModal

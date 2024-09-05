import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import { Box, Checkbox, IconButton, Typography } from '@mui/material';
import DeleteClaimDialog from './DeleteClaimDialog';
import { useEffect, useState } from 'react';
import services from '~/plugins/services';
import LoadingComponent from '~/components/LoadingComponent';
function MyClaim({ open, currentUser }) {
    const [selectedClaims, setSelectedClaims] = useState(null);
    const [myClaim, setMyClaim] = useState(null);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        setLoading(true);
    }, []);

    useEffect(() => {
        handleGetData();
    }, [loading])
    const handleGetData = async () => {
        try {
            await services.UserManagementAPI.getUserClaims(currentUser.id, (res) => {
                if (res.result.length !== 0) {
                    setMyClaim(res.result);
                }
            }, (err) => {
                console.log(err);
            });
            setLoading(false);
        } catch (error) {
            console.log(error);
            setLoading(false);
        }
    }
    const checkedClaim = (e, id) => {
        if (e.target.checked) {
            if (selectedClaims === null) setSelectedClaims([id]);
            else setSelectedClaims([id, ...selectedClaims])
        } else if (selectedClaims.length !== 1) {
            const filteredClaim = selectedClaims.filter((s) => {
                return s !== id;
            })
            setSelectedClaims(filteredClaim);
        } else {
            setSelectedClaims(null);
        }
    }
    const handleSelectAll = (e) => {
        if (e.target.checked) {
            setSelectedClaims([0, 1, 2, 3]);
        } else {
            setSelectedClaims(null);
        }
    }
    return (
        <Box mt="20px">
            <Box>
                {
                    myClaim === null ? (
                        <Typography>This user has not been assigned any claims yet.</Typography>
                    ) : (
                        <>
                            <Box sx={{ display: "flex", alignItems: "center" }}>
                                <Checkbox onClick={(e) => { handleSelectAll(e) }} /> <IconButton ><DeleteOutlineIcon /></IconButton>
                            </Box>
                            {
                                myClaim?.map((l, index) => {
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
                                                <Checkbox onChange={(e) => checkedClaim(e, index)} checked={!!selectedClaims?.includes(index)} /> {l.claimType} - {l.claimValue}
                                            </Box>
                                            <DeleteClaimDialog />
                                        </Box>
                                    )
                                })
                            }
                        </>
                    )
                }

            </Box>
            <LoadingComponent open={loading} setOpen={setLoading} />

        </Box>
    )
}

export default MyClaim

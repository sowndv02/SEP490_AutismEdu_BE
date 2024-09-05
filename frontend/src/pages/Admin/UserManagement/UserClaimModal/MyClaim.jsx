import { Box, Checkbox, Typography } from '@mui/material';
import { enqueueSnackbar } from 'notistack';
import { useEffect, useState } from 'react';
import LoadingComponent from '~/components/LoadingComponent';
import services from '~/plugins/services';
import DeleteClaimDialog from './DeleteClaimDialog';
function MyClaim({ currentUser }) {
    const [selectedClaims, setSelectedClaims] = useState([]);
    const [myClaim, setMyClaim] = useState(null);
    const [loading, setLoading] = useState(false);
    const [apiCall, setApiCall] = useState(null);
    useEffect(() => {
        setApiCall(1);
    }, []);

    useEffect(() => {
        if (apiCall !== null)
            setLoading(true);
    }, [apiCall]);

    useEffect(() => {
        if (apiCall === 1) {
            handleGetData();
        } else if (apiCall === 2) {
            handleDelete();
        }
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
            setApiCall(null);
        } catch (error) {
            console.log(error);
            setLoading(false);
        }
    }
    const handleDelete = async () => {
        try {
            await services.UserManagementAPI.removeUserClaims(currentUser.id,
                {
                    userId: currentUser.id,
                    userClaimIds: selectedClaims
                }, (res) => {
                    enqueueSnackbar("Remove claim successfully!", { variant: "success" });
                    const updatedClaim = myClaim.filter((m) => {
                        return !selectedClaims.includes(m.id);
                    });
                    setMyClaim(updatedClaim);
                }, (err) => {
                    enqueueSnackbar("Remove claim failed!", { variant: "error" });
                    console.log(err);
                });
            setLoading(false);
            setApiCall(null);
        } catch (error) {
            console.log(error);
            setLoading(false);
        }
    }

    const checkedClaim = (e, id) => {
        if (e.target.checked) {
            setSelectedClaims([id, ...selectedClaims])
        } else if (selectedClaims.length !== 1) {
            const filteredClaim = selectedClaims.filter((s) => {
                return s !== id;
            })
            setSelectedClaims(filteredClaim);
        } else {
            setSelectedClaims([]);
        }
    }
    const handleSelectAll = (e) => {
        if (e.target.checked) {
            const ids = myClaim.map((m) => {
                return m.id
            })
            setSelectedClaims(ids);
        } else {
            setSelectedClaims([]);
        }
    }
    return (
        <Box mt="20px" sx={{maxHeight:"500px", overflow:"auto"}}>
            <Box>
                {
                    (myClaim === null || myClaim.lenght !== 0) ? (
                        <Typography>This user has not been assigned any claims yet.</Typography>
                    ) : (
                        <>
                            <Box sx={{ display: "flex", alignItems: "center" }}>
                                <Checkbox onClick={(e) => { handleSelectAll(e) }}
                                    checked={Array.isArray(selectedClaims) && selectedClaims.length === myClaim?.length || 0} />
                                {
                                    Array.isArray(selectedClaims) && selectedClaims.length > 0 &&
                                    <DeleteClaimDialog setApiCall={setApiCall} numberClaim={selectedClaims.length} />
                                }

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
                                                <Checkbox onChange={(e) => checkedClaim(e, l.id)} checked={!!selectedClaims?.includes(l.id)} /> {l.claimType} - {l.claimValue}
                                            </Box>
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
